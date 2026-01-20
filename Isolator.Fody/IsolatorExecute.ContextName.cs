using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    List<string> _isolatorContextNames;
    MethodDefinition _setContextName;
    private void InjectSetContextName(MethodDefinition method, CustomAttribute isolatorCustomAttribute)
    {
        _isolatorContextNames ??= GetListOfUniqueIsolatorConstextNames();
        _setContextName ??= _targetType?.Methods.SingleOrDefault(_ => _.Name == "SetContextName");

        // Only inject SetContextName if there are any custom context names defined
        if (_isolatorContextNames.Count > 0)
        {
            var contextNameValue = GetFirstConstructorArgumentAsString(isolatorCustomAttribute);
            InjectMethodWithStringParameter(method, _setContextName, contextNameValue);

            WriteInfo($"Injected SetContextName with value: {contextNameValue} into method: {method.FullName}");
        }
    }

    private static string GetFirstConstructorArgumentAsString(CustomAttribute isolatorCustomAttribute)
    {
        var contextNameValue = isolatorCustomAttribute?.ConstructorArguments[0].Value as string;
        return contextNameValue ?? string.Empty;
    }

    private void FindContextNameMethod(string contextName)
    {
        if (_targetType is null)
            return;

        if (string.IsNullOrEmpty(contextName))
            return;

        var getContextNameMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "GetContextName");
        if (getContextNameMethod is not null)
        {
            // change GetContextName to return a constant string
            getContextNameMethod.Body.Instructions.Clear();
            getContextNameMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, contextName));
            getContextNameMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
    }

    private void FindLogMethod(bool enableDebug)
    {
        if (_targetType is null)
            return;

        if (enableDebug)
            return;

        var logMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "Log");
        if (logMethod is not null)
        {
            // change Log to do nothing
            logMethod.Body.Instructions.Clear();
            logMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
    }

    private List<string> GetListOfUniqueIsolatorConstextNames()
    {
        var isolatorCustomAttributes = ModuleDefinition.GetTypes()
            .Where(IsValidIsolatorTypeDefinition)
            .Select(e => e.CustomAttributes
            .SingleOrDefault(x => x.AttributeType.FullName == IsolatorAttribute))
            .OfType<CustomAttribute>();

        var isolatorCustomValues = isolatorCustomAttributes
            .SelectMany(e => e.ConstructorArguments.Select(arg => arg.Value))
            .OfType<string>()
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        //WriteMessage($"CustomAttribute: {isolatorCustomValues.Count} {string.Join(" ", isolatorCustomValues)}", MessageImportance.High);

        //foreach (var attribute in isolatorCustomAttributes)
        //{
        //    WriteMessage($"CustomAttribute: {attribute} {string.Join(" ", attribute.ConstructorArguments.Select(e => e.Value))}", MessageImportance.High);
        //}

        return isolatorCustomValues;
    }


    private static void InjectMethodWithStringParameter(MethodDefinition method, MethodDefinition methodWithStringParameter, string value)
    {
        var il = method.Body.GetILProcessor();
        var first = GetFirstInstructionAfterBaseConstructor(method);
        InjectMethodWithStringParameter(il, first, methodWithStringParameter, value);
    }
    private static void InjectMethodWithStringParameter(ILProcessor il, Instruction first, MethodDefinition methodWithStringParameter, string value)
    {
        if (methodWithStringParameter is null)
            return;

        if (string.IsNullOrEmpty(value))
        {
            il.InsertBefore(first, il.Create(OpCodes.Ldnull));
        }
        else
        {
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, value));
        }

        il.InsertBefore(first, il.Create(OpCodes.Call, methodWithStringParameter));
    }

}