using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;
using System;

public partial class ModuleWeaver
{
    MethodReference writeLine;
    public static string IsolatorAttribute { get; } = nameof(IsolatorAttribute);
    private void IsolatorExecute()
    {
        writeLine = ModuleDefinition.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) }));

        var config = new Configuration(Config);
        var typeNames = config.ClassNames;
        var intefaceNames = config.InterfaceNames;
        var cloneMethods = config.EnableCloneMethods;

        foreach (var type in ModuleDefinition.GetTypes())
        {
            // WriteMessage($"Type: {type} {string.Join(" ", type.CustomAttributes.Select(e => e.AttributeType))}", MessageImportance.High);

            if (!type.IsClass)
                continue;

            var needToIsolate = type.TryGetAndRemoveCustomAttribute(IsolatorAttribute);
            var equalToTypes = typeNames != null && typeNames.Count > 0 && typeNames.Contains(type.Name);
            var listOfInterfacesInType = GetInterfacesAndBaseInterfaces(type);
            var equalToInterfaces = intefaceNames != null &&
                intefaceNames.Count > 0 &&
                listOfInterfacesInType.Intersect(intefaceNames).Any();

            var skipIsolation = !needToIsolate && !equalToTypes && !equalToInterfaces;
            if (skipIsolation)
                continue;

            var index = 1;
            foreach (var method in type.Methods.ToArray())
            {
                if (!method.HasBody)
                    continue;

                var isolatorMethod = CloneMethod(type, method, $"_isolator_{index++}_");

                if (method.IsConstructor)
                {
                    InsjectConstructor_CreateInstance(method);
                    continue;
                }

                InjectMethod_InvokeMethod(method, isolatorMethod.Name);
            }
        }
    }

    private static bool logEnable = false;

    private void InsjectConstructor_CreateInstance(MethodDefinition method)
    {
        var body = method.Body;
        var il = body.GetILProcessor();
        var first = body.Instructions.First();

        // REQUIRED
        body.SimplifyMacros();
        body.InitLocals = true;

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

        // object CreateInstance(object key, params object[] args)
        var createInstanceMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "CreateInstance");
        if (createInstanceMethod != null)
        {
            var createInstanceMethodRef = ModuleDefinition.ImportReference(createInstanceMethod);
            // Create a local variable to store the result
            var instanceVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(instanceVariable);

            // Load key argument: 'this' for instance constructors, typeof(DeclaringType) for static constructors
            if (method.IsStatic)
            {
                var typeOfMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));
                il.InsertBefore(first, il.Create(OpCodes.Ldtoken, method.DeclaringType));
                il.InsertBefore(first, il.Create(OpCodes.Call, typeOfMethod));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // Load 'this' as the key argument
            }

            // Create parameters array using the helper
            var parametersArrayVariable = CreateParametersArray(method, il, first);

            il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersArrayVariable));
            il.InsertBefore(first, il.Create(OpCodes.Call, createInstanceMethodRef)); // Call CreateInstance(this or typeof, args)
            il.InsertBefore(first, il.Create(OpCodes.Stloc, instanceVariable)); // Store result in 'instance' variable

            // Write back ref/out parameters
            WriteBackRefOutParameters(method, il, first, parametersArrayVariable);
        }

        il.InsertBefore(first, il.Create(OpCodes.Ret));

        // REQUIRED
        method.Body.OptimizeMacros(); // This helps with stack issues
    }

    private void InjectMethod_InvokeMethod(MethodDefinition method, string searchMethodName = null)
    {
        var body = method.Body;
        var il = body.GetILProcessor();
        var first = body.Instructions.First();

        // REQUIRED
        body.SimplifyMacros();
        body.InitLocals = true;

        // Create a method that returns bool to control isolation
        var isolationControlMethod = CreateIsolationControlMethod();
        var isolationControlMethodRef = ModuleDefinition.ImportReference(isolationControlMethod);

        // Call the isolation control method
        var callIsolationControl = il.Create(OpCodes.Call, isolationControlMethodRef);
        il.InsertBefore(first, callIsolationControl);

        // Branch if false (skip isolation and continue normal execution)
        // When IsDefault() returns false, jump to original method body
        var branchIfFalse = il.Create(OpCodes.Brfalse_S, first);
        il.InsertBefore(first, branchIfFalse);

        var getInvokeMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "InvokeMethod");
        if (getInvokeMethod != null)
        {
            var getInvokeMethodRef = ModuleDefinition.ImportReference(getInvokeMethod);

            // Create parameters type array using the new method
            var parametersArrayVariable = CreateParametersArray(method, il, first);
            // Create parameters array using the new method
            var parametersTypeArrayVariable = CreateParametersTypeArray(method, il, first, searchMethodName);

            // Import System.Reflection types and methods
            var bindingFlagsType = ModuleDefinition.ImportReference(typeof(System.Reflection.BindingFlags));
            var makeByRefTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("MakeByRefType", Type.EmptyTypes));

            var resultVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(resultVariable);

            // Determine binding flags
            var bindingFlags = method.IsStatic
                ? System.Reflection.BindingFlags.Static
                : System.Reflection.BindingFlags.Instance;

            if (method.IsPrivate)
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else if (method.IsAssembly || method.IsFamilyAndAssembly)
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else
            {
                bindingFlags |= System.Reflection.BindingFlags.Public;
            }

            if (!string.IsNullOrEmpty(searchMethodName))
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
                bindingFlags &= ~System.Reflection.BindingFlags.Public;
            }

            // Create a local variable to store the result
            var returnValueVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(returnValueVariable);

            // Load key argument: 'this' for instance methods, typeof(DeclaringType) for static methods
            if (method.IsStatic)
            {
                var typeOfMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));
                il.InsertBefore(first, il.Create(OpCodes.Ldtoken, method.DeclaringType));
                il.InsertBefore(first, il.Create(OpCodes.Call, typeOfMethod));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldstr, searchMethodName ?? method.Name));
            il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersArrayVariable));
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, (int)bindingFlags));

            if (method.Parameters.Count > 0 && parametersTypeArrayVariable != null)
            {
                // Use pre-built Type[] array for parameter types
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersTypeArrayVariable));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));
            }

            il.InsertBefore(first, il.Create(OpCodes.Call, getInvokeMethodRef));
            il.InsertBefore(first, il.Create(OpCodes.Stloc, returnValueVariable));

            // Write back ref/out parameters
            if (new Configuration(Config).EnableWriteBackRefOutParameters)
                WriteBackRefOutParameters(method, il, first, parametersArrayVariable);

            // Handle return value
            if (method.ReturnType.FullName != "System.Void")
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, returnValueVariable));

                if (method.ReturnType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Unbox_Any, method.ReturnType));
                }
                else
                {
                    il.InsertBefore(first, il.Create(OpCodes.Castclass, method.ReturnType));
                }
            }
        }
        else
        {
            // GetData not found - return default value
            if (method.ReturnType.FullName != "System.Void")
            {
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

        // Return from isolation block (prevents fall-through to original method)
        il.InsertBefore(first, il.Create(OpCodes.Ret));

        // REQUIRED
        method.Body.OptimizeMacros(); // This helps with stack issues
    }

    private void InjectMethod(MethodDefinition method, string searchMethodName = null)
    {
        var body = method.Body;
        var il = body.GetILProcessor();
        var first = body.Instructions.First();

        // REQUIRED
        body.SimplifyMacros();
        body.InitLocals = true;

        // Create a method that returns bool to control isolation
        var isolationControlMethod = CreateIsolationControlMethod();
        var isolationControlMethodRef = ModuleDefinition.ImportReference(isolationControlMethod);

        // Call the isolation control method
        var callIsolationControl = il.Create(OpCodes.Call, isolationControlMethodRef);
        il.InsertBefore(first, callIsolationControl);

        // Branch if false (skip isolation and continue normal execution)
        // When IsDefault() returns false, jump to original method body
        var branchIfFalse = il.Create(OpCodes.Brfalse_S, first);
        il.InsertBefore(first, branchIfFalse);

        if (logEnable)
        {
            var message = $"[Fody] Entering {method.DeclaringType.FullName}.{method.Name}";
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));
            il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));
        }

        var getDataMethod = _targetType.Methods.SingleOrDefault(_ => _.Name == "GetData");
        if (getDataMethod != null)
        {
            var getDataMethodRef = ModuleDefinition.ImportReference(getDataMethod);

            // Create a local variable to store the result
            var dataVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
            method.Body.Variables.Add(dataVariable);

            // Load key argument: 'this' for instance methods, typeof(DeclaringType) for static methods
            if (method.IsStatic)
            {
                var typeOfMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));
                il.InsertBefore(first, il.Create(OpCodes.Ldtoken, method.DeclaringType));
                il.InsertBefore(first, il.Create(OpCodes.Call, typeOfMethod));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            }

            il.InsertBefore(first, il.Create(OpCodes.Call, getDataMethodRef));
            il.InsertBefore(first, il.Create(OpCodes.Stloc, dataVariable));

            // Create parameters type array using the new method
            var parametersArrayVariable = CreateParametersArray(method, il, first);
            // Create parameters array using the new method
            var parametersTypeArrayVariable = CreateParametersTypeArray(method, il, first, searchMethodName);

            // Import System.Reflection types and methods
            var typeType = ModuleDefinition.ImportReference(typeof(Type));
            var getTypeMethod = ModuleDefinition.ImportReference(typeof(object).GetMethod("GetType"));
            var getMethodWithTypesMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(System.Reflection.Binder), typeof(Type[]), typeof(System.Reflection.ParameterModifier[]) }));
            var getMethodWithoutTypesMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags) }));
            var methodInfoType = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodInfo));
            var invokeMethod = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }));

            var bindingFlagsType = ModuleDefinition.ImportReference(typeof(System.Reflection.BindingFlags));
            var makeByRefTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("MakeByRefType", Type.EmptyTypes));

            var methodInfoVariable = new VariableDefinition(methodInfoType);
            method.Body.Variables.Add(methodInfoVariable);

            // Determine binding flags
            var bindingFlags = method.IsStatic
                ? System.Reflection.BindingFlags.Static
                : System.Reflection.BindingFlags.Instance;

            if (method.IsPrivate)
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else if (method.IsAssembly || method.IsFamilyAndAssembly)
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else
            {
                bindingFlags |= System.Reflection.BindingFlags.Public;
            }

            if (!string.IsNullOrEmpty(searchMethodName))
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
                bindingFlags &= ~System.Reflection.BindingFlags.Public;
            }

            // Get the method using reflection
            if (method.IsStatic)
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable));
                il.InsertBefore(first, il.Create(OpCodes.Castclass, typeType));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable));
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getTypeMethod));
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldstr, searchMethodName ?? method.Name));
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, (int)bindingFlags));

            if (method.Parameters.Count > 0 && parametersTypeArrayVariable != null)
            {
                // Load null for Binder parameter
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));

                // Use pre-built Type[] array for parameter types
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersTypeArrayVariable));

                // Load null for ParameterModifier[] parameter
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getMethodWithTypesMethod));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getMethodWithoutTypesMethod));
            }

            il.InsertBefore(first, il.Create(OpCodes.Stloc, methodInfoVariable));

            if (logEnable)
            {
                var message = $"[Fody] MethodInfo retrieved for {method.DeclaringType.FullName}.{method.Name}: ";
                il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, methodInfoVariable));
                var objectToStringMethod = ModuleDefinition.ImportReference(typeof(object).GetMethod("ToString", Type.EmptyTypes));
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, objectToStringMethod));
                var stringConcatMethod = ModuleDefinition.ImportReference(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                il.InsertBefore(first, il.Create(OpCodes.Call, stringConcatMethod));
                il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));
            }

            //ForceToReturn(method, il, first); return;

            // Create parameters array using the new method
            //var parametersArrayVariable = CreateParametersArray(method, il, first);

            VariableDefinition returnValueVariable = null;
            if (method.ReturnType.FullName != "System.Void")
            {
                returnValueVariable = new VariableDefinition(ModuleDefinition.TypeSystem.Object);
                method.Body.Variables.Add(returnValueVariable);
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldloc, methodInfoVariable));

            if (method.IsStatic)
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable));
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersArrayVariable));
            il.InsertBefore(first, il.Create(OpCodes.Callvirt, invokeMethod));

            if (method.ReturnType.FullName != "System.Void")
            {
                il.InsertBefore(first, il.Create(OpCodes.Stloc, returnValueVariable));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Pop));
            }

            // Write back ref/out parameters
            if (new Configuration(Config).EnableWriteBackRefOutParameters)
                WriteBackRefOutParameters(method, il, first, parametersArrayVariable);

            // Handle return value
            if (method.ReturnType.FullName != "System.Void")
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, returnValueVariable));

                if (method.ReturnType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Unbox_Any, method.ReturnType));
                }
                else
                {
                    il.InsertBefore(first, il.Create(OpCodes.Castclass, method.ReturnType));
                }
            }
        }
        else
        {
            // GetData not found - return default value
            if (method.ReturnType.FullName != "System.Void")
            {
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

        // Return from isolation block (prevents fall-through to original method)
        il.InsertBefore(first, il.Create(OpCodes.Ret));

        // REQUIRED
        method.Body.OptimizeMacros(); // This helps with stack issues
    }

    private static void ForceToReturn(MethodDefinition method, ILProcessor il, Instruction first)
    {
        // GetData not found - return default value
        if (method.ReturnType.FullName != "System.Void")
        {
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

        // Return from isolation block (prevents fall-through to original method)
        il.InsertBefore(first, il.Create(OpCodes.Ret));

        // REQUIRED
        method.Body.OptimizeMacros(); // This helps with stack issues
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

    private static List<string> GetInterfacesAndBaseInterfaces(TypeDefinition type)
    {
        var listOfInterfacesInType = type.Interfaces.Select(i => i.InterfaceType.Name).ToList();
        // Add interfaces inside the base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            var baseTypeDef = baseType.Resolve();
            if (baseTypeDef != null)
            {
                foreach (var iface in baseTypeDef.Interfaces)
                {
                    var ifaceName = iface.InterfaceType.Name;
                    if (!listOfInterfacesInType.Contains(ifaceName))
                    {
                        listOfInterfacesInType.Add(ifaceName);
                    }
                }
                baseType = baseTypeDef.BaseType;
            }
            else
            {
                break;
            }
        }

        return listOfInterfacesInType;
    }
}