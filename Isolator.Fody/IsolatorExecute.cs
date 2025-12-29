using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;
using System;

public partial class ModuleWeaver
{
    public static string IsolatorAttribute { get; } = nameof(IsolatorAttribute);
    private void IsolatorExecute()
    {
        var consoleWriteLine = ModuleDefinition
            .ImportReference(typeof(Console).GetMethod(
                nameof(Console.WriteLine),
                new[] { typeof(string) }));

        foreach (var type in ModuleDefinition.GetTypes())
        {
            WriteMessage($"Type: {type} {string.Join(" ", type.CustomAttributes.Select(e => e.AttributeType))}", MessageImportance.High);

            if (!type.TryGetAndRemoveCustomAttribute(IsolatorAttribute))
                continue;

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                if (method.IsConstructor)
                    continue;

                InjectLog(method, consoleWriteLine);
            }
        }
    }

    private void InjectLog(MethodDefinition method, MethodReference writeLine)
    {
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions.First();

        var message = $"[Fody] Entering {method.DeclaringType.FullName}.{method.Name}";

        // Load true onto the stack for the conditional
        var loadTrue = il.Create(OpCodes.Ldc_I4_1);
        il.InsertBefore(first, loadTrue);

        // Create the target instruction for the false branch (continue normal execution)
        var continueNormalExecution = first;

        // Branch if false (skip log and return)
        var branchIfFalse = il.Create(OpCodes.Brfalse_S, continueNormalExecution);
        il.InsertBefore(first, branchIfFalse);

        // Log message
        il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));
        il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));

        // Return immediately after log injection
        if (method.ReturnType.FullName != "System.Void")
        {
            // Return default value for non-void methods
            if (method.ReturnType.IsValueType)
            {
                var variable = new VariableDefinition(method.ReturnType);
                method.Body.Variables.Add(variable);
                il.InsertBefore(first, il.Create(OpCodes.Ldloca_S, variable));
                il.InsertBefore(first, il.Create(OpCodes.Initobj, method.ReturnType));
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, variable));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));
            }
        }

        il.InsertBefore(first, il.Create(OpCodes.Ret));
    }
}
