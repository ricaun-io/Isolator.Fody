using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    private const string AttachMethodName = "IsolatorUtility::Initialize()";
    private void CallAttach(Configuration config)
    {
        if (_attachMethod is null)
        {
            WriteMessage($"AttachMethod is null and CallAttach is ignored.", MessageImportance.High);
            return;
        }

        var loadAtModuleInit = config.LoadAtModuleInit;
        var initialized = FindInitializeCalls();

        if (loadAtModuleInit)
        {
            AddModuleInitializerCall();
        }
        //else if (!initialized)
        //{
        //    throw new WeavingException($"Isolator was not initialized. Make sure LoadAtModuleInit=true or call {AttachMethodName}.");
        //}
    }

    private bool FindInitializeCalls()
    {
        var found = false;

        foreach (var type in ModuleDefinition.Types)
        {
            if (!type.HasMethods)
            {
                continue;
            }

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                var instructions = method.Body.Instructions;
                for (var i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];
                    if (instruction.OpCode != OpCodes.Call)
                    {
                        continue;
                    }

                    if (instruction.Operand is not MethodReference callMethod)
                    {
                        continue;
                    }

                    if (callMethod.FullName == $"System.Void {AttachMethodName}")
                    {
                        found = true;

                        instructions[i] = Instruction.Create(OpCodes.Call, _attachMethod);
                    }
                }
            }
        }

        return found;
    }

    private void AddModuleInitializerCall()
    {
        const MethodAttributes attributes = MethodAttributes.Private
                                            | MethodAttributes.HideBySig
                                            | MethodAttributes.Static
                                            | MethodAttributes.SpecialName
                                            | MethodAttributes.RTSpecialName;

        var moduleClass = ModuleDefinition.Types.FirstOrDefault(_ => _.Name == "<Module>");
        if (moduleClass is null)
        {
            throw new WeavingException("Found no module class!");
        }

        var cctor = moduleClass.Methods.FirstOrDefault(_ => _.Name == ".cctor");
        if (cctor is null)
        {
            cctor = new MethodDefinition(".cctor", attributes, TypeSystem.VoidReference);
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            moduleClass.Methods.Add(cctor);
        }

        cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, _attachMethod));
    }
}
