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
            // WriteMessage($"Type: {type} {string.Join(" ", type.CustomAttributes.Select(e => e.AttributeType))}", MessageImportance.High);

            if (!type.TryGetAndRemoveCustomAttribute(IsolatorAttribute))
                continue;

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                if (method.IsConstructor)
                {
                    InsjectConstructor(method, consoleWriteLine);
                    continue;
                }

                InjectMethod(method, consoleWriteLine);
            }
        }
    }

    private static bool logEnable = false;

    private void InsjectConstructor(MethodDefinition method, MethodReference writeLine)
    {
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions.First();
        // Create a method that returns bool to control isolation
        var isolationControlMethod = CreateIsolationControlMethod();
        var isolationControlMethodRef = ModuleDefinition.ImportReference(isolationControlMethod);
        // Call the isolation control method
        il.InsertBefore(first, il.Create(OpCodes.Call, isolationControlMethodRef));
        // Create the target instruction for the false branch (continue normal execution)
        var continueNormalExecution = first;
        // Branch if false (skip log and return)
        var branchIfFalse = il.Create(OpCodes.Brfalse_S, continueNormalExecution);
        il.InsertBefore(first, branchIfFalse);

        if (logEnable)
        {
            var message = $"[Fody] Entering constructor {method.DeclaringType.FullName}";
            // Log message
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));
            il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));
        }

        // object CreateInstance(object key, params object[] args)
        var createInstanceMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "CreateInstance");
        if (createInstanceMethod != null)
        {
            var createInstanceMethodRef = ModuleDefinition.ImportReference(createInstanceMethod);
            // Create a local variable to store the result
            var instanceVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(instanceVariable);

            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // Load 'this' as the key argument

            // Create array with correct size for all constructor parameters (excluding 'this')
            var parameterCount = method.Parameters.Count;
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, parameterCount)); // Load parameter count
            il.InsertBefore(first, il.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object)); // Create object array

            // Load each constructor argument into the array
            for (int i = 0; i < parameterCount; i++)
            {
                il.InsertBefore(first, il.Create(OpCodes.Dup)); // Duplicate array reference
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, i)); // Load array index
                il.InsertBefore(first, il.Create(OpCodes.Ldarg, i + 1)); // Load constructor argument (arg0, arg1, etc. - +1 to skip 'this')

                // Box value types
                if (method.Parameters[i].ParameterType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Box, method.Parameters[i].ParameterType));
                }

                il.InsertBefore(first, il.Create(OpCodes.Stelem_Ref)); // Store in array
            }

            il.InsertBefore(first, il.Create(OpCodes.Call, createInstanceMethodRef)); // Call CreateInstance(this, args)
            il.InsertBefore(first, il.Create(OpCodes.Stloc, instanceVariable)); // Store result in 'instance' variable
        }

        il.InsertBefore(first, il.Create(OpCodes.Ret));
    }

    private void InjectMethod(MethodDefinition method, MethodReference writeLine)
    {
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions.First();

        // Create a method that returns bool to control isolation
        var isolationControlMethod = CreateIsolationControlMethod();
        var isolationControlMethodRef = ModuleDefinition.ImportReference(isolationControlMethod);

        // Call the isolation control method
        il.InsertBefore(first, il.Create(OpCodes.Call, isolationControlMethodRef));

        // Create the target instruction for the false branch (continue normal execution)
        var continueNormalExecution = first;

        // Branch if false (skip log and return)
        var branchIfFalse = il.Create(OpCodes.Brfalse_S, continueNormalExecution);
        il.InsertBefore(first, branchIfFalse);

        if (logEnable)
        {
            var message = $"[Fody] Entering {method.DeclaringType.FullName}.{method.Name}";
            // Log message
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));
            il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));
        }

        var getDataMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "GetData");
        // Call object GetData(object key) like var 'data = GetData(this)';
        if (getDataMethod != null)
        {
            var getDataMethodRef = ModuleDefinition.ImportReference(getDataMethod);

            // Create a local variable to store the result
            var dataVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(dataVariable);

            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // Load 'this' as the key argument
            il.InsertBefore(first, il.Create(OpCodes.Call, getDataMethodRef)); // Call GetData(this)
            il.InsertBefore(first, il.Create(OpCodes.Stloc, dataVariable)); // Store result in 'data' variable

            // Import System.Reflection types and methods
            var typeType = ModuleDefinition.ImportReference(typeof(Type));
            var getTypeMethod = ModuleDefinition.ImportReference(typeof(object).GetMethod("GetType"));
            var getMethodMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string) }));
            var methodInfoType = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodInfo));
            var invokeMethod = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }));

            // Create local variable for MethodInfo
            var methodInfoVariable = new VariableDefinition(methodInfoType);
            method.Body.Variables.Add(methodInfoVariable);

            // Get the method using reflection: data.GetType().GetMethod(method.Name)
            il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable)); // Load 'data'
            il.InsertBefore(first, il.Create(OpCodes.Callvirt, getTypeMethod)); // Call data.GetType()
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, method.Name)); // Load method name
            il.InsertBefore(first, il.Create(OpCodes.Callvirt, getMethodMethod)); // Call GetMethod(methodName)
            il.InsertBefore(first, il.Create(OpCodes.Stloc, methodInfoVariable)); // Store MethodInfo

            // Invoke: methodInfo.Invoke(data, parameters)
            il.InsertBefore(first, il.Create(OpCodes.Ldloc, methodInfoVariable)); // Load MethodInfo
            il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable)); // Load 'data' as target object

            // Create array with correct size for all method parameters (excluding 'this')
            var parameterCount = method.Parameters.Count;
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, parameterCount)); // Load parameter count
            il.InsertBefore(first, il.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object)); // Create object array

            // Load each method argument into the array
            for (int i = 0; i < parameterCount; i++)
            {
                il.InsertBefore(first, il.Create(OpCodes.Dup)); // Duplicate array reference
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, i)); // Load array index
                il.InsertBefore(first, il.Create(OpCodes.Ldarg, i + 1)); // Load method argument (arg0, arg1, etc. - +1 to skip 'this')

                // Box value types
                if (method.Parameters[i].ParameterType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Box, method.Parameters[i].ParameterType));
                }

                il.InsertBefore(first, il.Create(OpCodes.Stelem_Ref)); // Store in array
            }

            il.InsertBefore(first, il.Create(OpCodes.Callvirt, invokeMethod)); // Call methodInfo.Invoke(data, args)

            // Handle return value
            if (method.ReturnType.FullName != "System.Void")
            {
                // Unbox/cast the result to the correct return type
                if (method.ReturnType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Unbox_Any, method.ReturnType));
                }
                else
                {
                    il.InsertBefore(first, il.Create(OpCodes.Castclass, method.ReturnType));
                }
            }
            else
            {
                // Pop the return value if method is void
                il.InsertBefore(first, il.Create(OpCodes.Pop));
            }
        }
        else
        {
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
        }

        il.InsertBefore(first, il.Create(OpCodes.Ret));
    }

    private MethodDefinition CreateIsolationControlMethod()
    {
        var shouldIsolateMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "IsDefault");

        if (shouldIsolateMethod == null)
        {
            throw new WeavingException($"IsDefault method not found on type {_targetType.FullName}");
        }

        return shouldIsolateMethod;
    }
}
