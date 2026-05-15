using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    private TypeDefinition isolatorAssemblyLoadContext;

    private void UpdateCollectibleContext(bool enableCollectible = false)
    {
        if (_targetType is null)
            return;

        isolatorAssemblyLoadContext ??= _targetType.NestedTypes.SingleOrDefault(t => t.Name == "IsolatorAssemblyLoadContext");

        var collectibleField = isolatorAssemblyLoadContext?.Fields.SingleOrDefault(f => f.Name == "Collectible");
        if (collectibleField == null)
            return;

        // ensure field is static and not readonly so we can set its value
        collectibleField.IsStatic = true;
        collectibleField.IsInitOnly = false;

        // If the field is not a literal, ensure the static constructor sets the desired default.
        if (!collectibleField.IsLiteral)
        {
            WriteInfo($"Replace Collectible field with value: {enableCollectible} into IsolatorAssemblyLoadContext");

            var module = isolatorAssemblyLoadContext.Module;
            var cctor = isolatorAssemblyLoadContext.Methods.SingleOrDefault(m => m.IsConstructor && m.IsStatic);

            if (cctor == null)
            {
                var methodAttrs = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                cctor = new MethodDefinition(".cctor", methodAttrs, module.TypeSystem.Void);
                isolatorAssemblyLoadContext.Methods.Add(cctor);

                var il = cctor.Body.GetILProcessor();
                il.Append(Instruction.Create(enableCollectible ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                il.Append(Instruction.Create(OpCodes.Stsfld, collectibleField));
                il.Append(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                var il = cctor.Body.GetILProcessor();
                var instructions = cctor.Body.Instructions.ToArray();
                bool replaced = false;

                for (int i = 0; i < instructions.Length; i++)
                {
                    var instr = instructions[i];
                    if (instr.OpCode == OpCodes.Stsfld && instr.Operand is FieldReference fr && fr.Name == collectibleField.Name)
                    {
                        replaced = true;
                        // Replace or insert the load boolean instruction before stsfld
                        Instruction load = enableCollectible ? Instruction.Create(OpCodes.Ldc_I4_1) : Instruction.Create(OpCodes.Ldc_I4_0);
                        var index = cctor.Body.Instructions.IndexOf(instr);
                        if (index > 0)
                        {
                            var prev = cctor.Body.Instructions[index - 1];
                            // If previous is a load of an int, replace it; otherwise insert
                            if (prev.OpCode == OpCodes.Ldc_I4_0 || prev.OpCode == OpCodes.Ldc_I4_1 || prev.OpCode == OpCodes.Ldc_I4_2 || prev.OpCode == OpCodes.Ldc_I4 || prev.OpCode == OpCodes.Ldc_I4_S)
                                il.Replace(prev, load);
                            else
                                il.InsertBefore(instr, load);
                        }
                        else
                        {
                            il.InsertBefore(instr, load);
                        }

                        // ensure operand references the field definition
                        instr.Operand = collectibleField;
                    }
                }

                if (!replaced)
                {
                    // no existing assignment found - insert at start (before first instruction) or append if empty
                    var first = cctor.Body.Instructions.FirstOrDefault();
                    var load = enableCollectible ? Instruction.Create(OpCodes.Ldc_I4_1) : Instruction.Create(OpCodes.Ldc_I4_0);
                    if (first != null)
                    {
                        il.InsertBefore(first, load);
                        il.InsertAfter(load, Instruction.Create(OpCodes.Stsfld, collectibleField));
                    }
                    else
                    {
                        il.Append(load);
                        il.Append(Instruction.Create(OpCodes.Stsfld, collectibleField));
                        il.Append(Instruction.Create(OpCodes.Ret));
                    }
                }
            }
        }

    }

}