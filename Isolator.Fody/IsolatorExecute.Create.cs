using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

public partial class ModuleWeaver
{
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
            _ when type.IsValueType => Instruction.Create(OpCodes.Ldobj, type),
            _ => Instruction.Create(OpCodes.Ldind_Ref),
        };
    }

    static Instruction CreateStind(TypeReference type)
    {
        return type.MetadataType switch
        {
            MetadataType.Boolean or MetadataType.SByte or MetadataType.Byte => Instruction.Create(OpCodes.Stind_I1),
            MetadataType.Int16 or MetadataType.UInt16 => Instruction.Create(OpCodes.Stind_I2),
            MetadataType.Int32 or MetadataType.UInt32 => Instruction.Create(OpCodes.Stind_I4),
            MetadataType.Int64 or MetadataType.UInt64 => Instruction.Create(OpCodes.Stind_I8),
            MetadataType.Single => Instruction.Create(OpCodes.Stind_R4),
            MetadataType.Double => Instruction.Create(OpCodes.Stind_R8),
            MetadataType.IntPtr or MetadataType.UIntPtr => Instruction.Create(OpCodes.Stind_I),
            _ when type.IsValueType => Instruction.Create(OpCodes.Stobj, type),
            _ => Instruction.Create(OpCodes.Stind_Ref),
        };
    }

    static int GetArgumentIndex(MethodDefinition method, ParameterDefinition parameter)
    {
        return parameter.Index + (method.HasThis ? 1 : 0);
    }

    private VariableDefinition CreateParametersTypeArray(
        MethodDefinition method,
        ILProcessor il,
        Instruction insertBefore)
    {
        int parameterCount = method.Parameters.Count;
        if (parameterCount == 0)
        {
            return null;
        }

        // Check if Type contains only a single method with the given name
        var methodName = method.Name;
        var typeDefinition = method.DeclaringType.Resolve();
        var hasSingleMethod = typeDefinition.Methods.Count(m => m.Name == methodName) == 1;
        if (hasSingleMethod)
        {
            return null;
        }

        var module = method.Module;
        var body = method.Body;

        body.InitLocals = true;

        var systemTypeReference = module.ImportReference(typeof(System.Type));
        var typeArrayVariable = new VariableDefinition(new ArrayType(systemTypeReference));
        body.Variables.Add(typeArrayVariable);

        var getTypeFromHandle = module.ImportReference(
            typeof(System.Type).GetMethod(nameof(System.Type.GetTypeFromHandle), new[] { typeof(System.RuntimeTypeHandle) }));

        var makeByRefTypeMethod = module.ImportReference(typeof(System.Type)
            .GetMethod(nameof(System.Type.MakeByRefType), System.Type.EmptyTypes));

        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldc_I4, parameterCount));
        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Newarr, systemTypeReference));
        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Stloc, typeArrayVariable));

        for (int i = 0; i < parameterCount; i++)
        {
            var parameterType = method.Parameters[i].ParameterType;
            var isByReference = parameterType.IsByReference;
            var typeForToken = isByReference ? parameterType.GetElementType() : parameterType;

            il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldloc, typeArrayVariable));
            il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldc_I4, i));
            il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldtoken, typeForToken));
            il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Call, getTypeFromHandle));

            if (isByReference)
            {
                il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Callvirt, makeByRefTypeMethod));
            }

            il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Stelem_Ref));
        }

        return typeArrayVariable;
    }

    private VariableDefinition CreateParametersArray(
    MethodDefinition method,
    ILProcessor il,
    Instruction insertBefore)
    {
        var module = method.Module;
        var body = method.Body;

        body.InitLocals = true;

        // object[] local
        var objectArray = new VariableDefinition(
            new ArrayType(module.TypeSystem.Object));

        body.Variables.Add(objectArray);

        int paramCount = method.Parameters.Count;

        // new object[paramCount]
        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Ldc_I4, paramCount));
        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Newarr, module.TypeSystem.Object));
        il.InsertBefore(insertBefore, Instruction.Create(OpCodes.Stloc, objectArray));

        // Populate array – IMPORTANT: one complete stack sequence per parameter
        for (int i = 0; i < paramCount; i++)
        {
            var param = method.Parameters[i];

            bool isByRef = param.ParameterType.IsByReference;

            TypeReference valueType = isByRef
                ? ((ByReferenceType)param.ParameterType).ElementType
                : param.ParameterType;

            int argIndex = method.HasThis ? i + 1 : i;

            // args[i] = (object)value;

            // array
            il.InsertBefore(insertBefore,
                Instruction.Create(OpCodes.Ldloc, objectArray));

            // index
            il.InsertBefore(insertBefore,
                Instruction.Create(OpCodes.Ldc_I4, i));

            // load argument
            il.InsertBefore(insertBefore,
                CreateLdarg(argIndex));

            // dereference ref/out
            if (isByRef)
            {
                il.InsertBefore(insertBefore,
                    CreateLdind(valueType));
            }

            // box value types & generics
            if (valueType.IsValueType || valueType.IsGenericParameter)
            {
                il.InsertBefore(insertBefore,
                    Instruction.Create(OpCodes.Box,
                        module.ImportReference(valueType)));
            }

            // store
            il.InsertBefore(insertBefore,
                Instruction.Create(OpCodes.Stelem_Ref));
        }

        return objectArray;
    }

    private static Instruction CreateLdarg(int index)
    {
        return index switch
        {
            0 => Instruction.Create(OpCodes.Ldarg_0),
            1 => Instruction.Create(OpCodes.Ldarg_1),
            2 => Instruction.Create(OpCodes.Ldarg_2),
            3 => Instruction.Create(OpCodes.Ldarg_3),
            <= byte.MaxValue => Instruction.Create(OpCodes.Ldarg_S, (byte)index),
            _ => Instruction.Create(OpCodes.Ldarg, index)
        };
    }

    private void WriteBackRefOutParameters(MethodDefinition method, ILProcessor il, Instruction insertBefore, VariableDefinition parametersArrayVariable)
    {
        var parameterCount = method.Parameters.Count;

        for (int i = 0; i < parameterCount; i++)
        {
            var paramType = method.Parameters[i].ParameterType;
            if (paramType.IsByReference)
            {
                var elementType = paramType.GetElementType();

                il.InsertBefore(insertBefore, il.Create(OpCodes.Ldarg, GetArgumentIndex(method, method.Parameters[i])));
                il.InsertBefore(insertBefore, il.Create(OpCodes.Ldloc, parametersArrayVariable));
                il.InsertBefore(insertBefore, il.Create(OpCodes.Ldc_I4, i));
                il.InsertBefore(insertBefore, il.Create(OpCodes.Ldelem_Ref));

                if (elementType.IsValueType)
                {
                    il.InsertBefore(insertBefore, il.Create(OpCodes.Unbox_Any, elementType));
                }
                else
                {
                    il.InsertBefore(insertBefore, il.Create(OpCodes.Castclass, elementType));
                }

                il.InsertBefore(insertBefore, CreateStind(elementType));
            }
        }
    }
}