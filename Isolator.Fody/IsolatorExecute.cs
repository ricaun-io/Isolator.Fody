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

            // Create array with correct size for all constructor parameters (excluding 'this')
            var parameterCount = method.Parameters.Count;
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, parameterCount)); // Load parameter count
            il.InsertBefore(first, il.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object)); // Create object array

            // Load each constructor argument into the array
            var argOffset = method.IsStatic ? 0 : 1; // Static constructors start at arg0, instance constructors start at arg1
            for (int i = 0; i < parameterCount; i++)
            {
                il.InsertBefore(first, il.Create(OpCodes.Dup)); // Duplicate array reference
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, i)); // Load array index
                il.InsertBefore(first, il.Create(OpCodes.Ldarg, i + argOffset)); // Load constructor argument

                var paramType = method.Parameters[i].ParameterType;
                if (paramType.IsByReference)
                {
                    paramType = paramType.GetElementType();
                    il.InsertBefore(first, CreateLdind(paramType));
                }
                // Box value types
                if (paramType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Box, ModuleDefinition.ImportReference(paramType)));
                }

                il.InsertBefore(first, il.Create(OpCodes.Stelem_Ref)); // Store in array
            }

            il.InsertBefore(first, il.Create(OpCodes.Call, createInstanceMethodRef)); // Call CreateInstance(this or typeof, args)
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

            // Load key argument: 'this' for instance methods, typeof(DeclaringType) for static methods
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

            il.InsertBefore(first, il.Create(OpCodes.Call, getDataMethodRef)); // Call GetData(this or typeof)
            il.InsertBefore(first, il.Create(OpCodes.Stloc, dataVariable)); // Store result in 'data' variable

            // Import System.Reflection types and methods
            var typeType = ModuleDefinition.ImportReference(typeof(Type));
            var getTypeMethod = ModuleDefinition.ImportReference(typeof(object).GetMethod("GetType"));
            var getMethodWithTypesMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(System.Reflection.Binder), typeof(Type[]), typeof(System.Reflection.ParameterModifier[]) }));
            var getMethodWithoutTypesMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags) }));
            var methodInfoType = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodInfo));
            var invokeMethod = ModuleDefinition.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }));

            // Import BindingFlags
            var bindingFlagsType = ModuleDefinition.ImportReference(typeof(System.Reflection.BindingFlags));

            // Import Type.MakeByRefType method
            var makeByRefTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("MakeByRefType", Type.EmptyTypes));

            // Create local variable for MethodInfo
            var methodInfoVariable = new VariableDefinition(methodInfoType);
            method.Body.Variables.Add(methodInfoVariable);

            // Determine binding flags based on method visibility and static/instance nature
            var bindingFlags = method.IsStatic
                ? System.Reflection.BindingFlags.Static
                : System.Reflection.BindingFlags.Instance;

            if (method.IsPrivate)
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else if (method.IsAssembly || method.IsFamilyAndAssembly) // internal
            {
                bindingFlags |= System.Reflection.BindingFlags.NonPublic;
            }
            else
            {
                bindingFlags |= System.Reflection.BindingFlags.Public;
            }

            // Get the method using reflection
            // For static methods: data is already Type, use it directly
            // For instance methods: data is object instance, call GetType()
            if (method.IsStatic)
            {
                // data is Type, cast and use directly: ((Type)data).GetMethod(methodName, bindingFlags, ...)
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable)); // Load 'data'
                il.InsertBefore(first, il.Create(OpCodes.Castclass, typeType)); // Cast to Type
            }
            else
            {
                // data is object instance, call GetType(): data.GetType().GetMethod(methodName, bindingFlags, ...)
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable)); // Load 'data'
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getTypeMethod)); // Call data.GetType()
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldstr, method.Name)); // Load method name
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, (int)bindingFlags)); // Load binding flags

            // If method has parameters, use GetMethod overload with Type[] parameter types
            if (method.Parameters.Count > 0)
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldnull)); // Binder (null for default)

                // Create Type[] array for parameter types
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, method.Parameters.Count));
                il.InsertBefore(first, il.Create(OpCodes.Newarr, typeType));

                // Populate Type[] array with parameter types
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Dup)); // Duplicate array reference
                    il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, i)); // Load array index

                    var paramType = method.Parameters[i].ParameterType;

                    // Handle ref/out parameters by getting the element type and calling MakeByRefType
                    if (paramType.IsByReference)
                    {
                        paramType = paramType.GetElementType();
                        il.InsertBefore(first, il.Create(OpCodes.Ldtoken, paramType)); // Load parameter type token
                        var getTypeFromHandleMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));
                        il.InsertBefore(first, il.Create(OpCodes.Call, getTypeFromHandleMethod)); // Get Type from token
                        il.InsertBefore(first, il.Create(OpCodes.Callvirt, makeByRefTypeMethod)); // Call MakeByRefType
                    }
                    else
                    {
                        il.InsertBefore(first, il.Create(OpCodes.Ldtoken, paramType)); // Load parameter type token
                        var getTypeFromHandleMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));
                        il.InsertBefore(first, il.Create(OpCodes.Call, getTypeFromHandleMethod)); // Get Type from token
                    }

                    il.InsertBefore(first, il.Create(OpCodes.Stelem_Ref)); // Store in array
                }

                il.InsertBefore(first, il.Create(OpCodes.Ldnull)); // ParameterModifier[] (null)
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getMethodWithTypesMethod)); // Call GetMethod with types
            }
            else
            {
                // No parameters, use simple GetMethod overload
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, getMethodWithoutTypesMethod)); // Call GetMethod(methodName, bindingFlags)
            }

            il.InsertBefore(first, il.Create(OpCodes.Stloc, methodInfoVariable)); // Store MethodInfo

            if (logEnable)
            {
                var message = $"[Fody] MethodInfo retrieved for {method.DeclaringType.FullName}.{method.Name}: ";
                // Log message prefix
                il.InsertBefore(first, il.Create(OpCodes.Ldstr, message));

                // Load the methodInfoVariable and convert to string
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, methodInfoVariable));

                // Call object.ToString() on the MethodInfo
                var objectToStringMethod = ModuleDefinition.ImportReference(typeof(object).GetMethod("ToString", Type.EmptyTypes));
                il.InsertBefore(first, il.Create(OpCodes.Callvirt, objectToStringMethod));

                // Concatenate the strings
                var stringConcatMethod = ModuleDefinition.ImportReference(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                il.InsertBefore(first, il.Create(OpCodes.Call, stringConcatMethod));

                // Write to console
                il.InsertBefore(first, il.Create(OpCodes.Call, writeLine));
            }

            // Create local variable for array of method parameters
            var parametersArrayVariable = new VariableDefinition(new ArrayType(ModuleDefinition.TypeSystem.Object));
            method.Body.Variables.Add(parametersArrayVariable);

            // Create array with correct size for all method parameters (excluding 'this' for instance methods)
            var parameterCount = method.Parameters.Count;
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, parameterCount)); // Load parameter count
            il.InsertBefore(first, il.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object)); // Create object array

            // Load each method argument into the array
            var argOffset = method.IsStatic ? 0 : 1; // Static methods start at arg0, instance methods start at arg1
            for (int i = 0; i < parameterCount; i++)
            {
                il.InsertBefore(first, il.Create(OpCodes.Dup)); // Duplicate array reference
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, i)); // Load array index
                il.InsertBefore(first, il.Create(OpCodes.Ldarg, i + argOffset)); // Load method argument

                var paramType = method.Parameters[i].ParameterType;
                if (paramType.IsByReference)
                {
                    paramType = paramType.GetElementType();
                    il.InsertBefore(first, CreateLdind(paramType));
                }
                // Box value types
                if (paramType.IsValueType)
                {
                    il.InsertBefore(first, il.Create(OpCodes.Box, ModuleDefinition.ImportReference(paramType)));
                }

                il.InsertBefore(first, il.Create(OpCodes.Stelem_Ref)); // Store in array
            }

            // Store the parameters array for potential ref/out parameter updates
            il.InsertBefore(first, il.Create(OpCodes.Stloc, parametersArrayVariable)); // Store array reference

            // Invoke: methodInfo.Invoke(data, parameters)
            il.InsertBefore(first, il.Create(OpCodes.Ldloc, methodInfoVariable)); // Load MethodInfo

            // For static methods, pass null as target; for instance methods, pass 'data'
            if (method.IsStatic)
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldnull));
            }
            else
            {
                il.InsertBefore(first, il.Create(OpCodes.Ldloc, dataVariable)); // Load 'data' as target object
            }

            il.InsertBefore(first, il.Create(OpCodes.Ldloc, parametersArrayVariable)); // Load parameters array
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

    static Instruction CreateLdind(TypeReference type)
    {
        return type.MetadataType switch
        {
            MetadataType.Boolean or MetadataType.SByte => Instruction.Create(OpCodes.Ldind_I1),
            MetadataType.Byte => Instruction.Create(OpCodes.Ldind_U1),
            MetadataType.Int16 => Instruction.Create(OpCodes.Ldind_I2),
            MetadataType.UInt16 => Instruction.Create(OpCodes.Ldind_U2),
            MetadataType.Int32 => Instruction.Create(OpCodes.Ldind_I4),
            MetadataType.UInt32 => Instruction.Create(OpCodes.Ldind_U4),
            MetadataType.Int64 or MetadataType.UInt64 => Instruction.Create(OpCodes.Ldind_I8),
            MetadataType.Single => Instruction.Create(OpCodes.Ldind_R4),
            MetadataType.Double => Instruction.Create(OpCodes.Ldind_R8),
            MetadataType.IntPtr or MetadataType.UIntPtr => Instruction.Create(OpCodes.Ldind_I),
            _ => Instruction.Create(OpCodes.Ldind_Ref),
        };
    }
}