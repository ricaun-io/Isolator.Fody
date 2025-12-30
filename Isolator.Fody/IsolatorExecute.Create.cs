using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;
using System;

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

    private VariableDefinition CreateParametersArray(MethodDefinition method, ILProcessor il, Instruction insertBefore)
    {
        var parametersArrayVariable = new VariableDefinition(new ArrayType(ModuleDefinition.TypeSystem.Object));
        method.Body.Variables.Add(parametersArrayVariable);

        var parameterCount = method.Parameters.Count;
        il.InsertBefore(insertBefore, il.Create(OpCodes.Ldc_I4, parameterCount));
        il.InsertBefore(insertBefore, il.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object));
        il.InsertBefore(insertBefore, il.Create(OpCodes.Stloc, parametersArrayVariable));

        for (int i = 0; i < parameterCount; i++)
        {
            var parameter = method.Parameters[i];

            il.InsertBefore(insertBefore, il.Create(OpCodes.Ldloc, parametersArrayVariable));
            il.InsertBefore(insertBefore, il.Create(OpCodes.Ldc_I4, i));

            var paramType = parameter.ParameterType;
            var actualType = paramType.IsByReference ? paramType.GetElementType() : paramType;

            if (parameter.IsOut && !parameter.IsIn)
            {
                il.InsertBefore(insertBefore, il.Create(OpCodes.Ldnull));
                il.InsertBefore(insertBefore, il.Create(OpCodes.Stelem_Ref));
                continue;
            }

            il.InsertBefore(insertBefore, il.Create(OpCodes.Ldarg, GetArgumentIndex(method, parameter)));

            if (paramType.IsByReference)
            {
                il.InsertBefore(insertBefore, CreateLdind(actualType));
            }
            if (actualType.IsValueType)
            {
                il.InsertBefore(insertBefore, il.Create(OpCodes.Box, ModuleDefinition.ImportReference(actualType)));
            }

            il.InsertBefore(insertBefore, il.Create(OpCodes.Stelem_Ref));
        }

        return parametersArrayVariable;
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