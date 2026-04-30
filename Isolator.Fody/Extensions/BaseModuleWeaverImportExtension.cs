using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Linq;

public static class BaseModuleWeaverImportExtension
{
    internal static TypeDefinition ImportTypeDefinition(this BaseModuleWeaver weaver, Type type)
    {
        // Unwrap array → get element type
        if (type.IsArray)
            type = type.GetElementType()!;

        // Optional: also unwrap ref/pointer (usually a good idea)
        if (type.IsByRef || type.IsPointer)
            type = type.GetElementType()!;

        return weaver.FindTypeDefinition(type.FullName) ?? weaver.FindTypeDefinition(type.Name);
    }

    public static TypeReference ImportTypeReference(this BaseModuleWeaver weaver, Type type)
    {
        var typeDef = weaver.ImportTypeDefinition(type);

        if (type.IsArray)
        {
            var elementType = weaver.ModuleDefinition.ImportReference(typeDef);
            return new ArrayType(elementType, type.GetArrayRank());

        }
        else if (type.IsByRef)
        {
            var elementType = weaver.ModuleDefinition.ImportReference(typeDef);
            return new ByReferenceType(elementType);
        }
        else if (type.IsPointer)
        {
            var elementType = weaver.ModuleDefinition.ImportReference(typeDef);
            return new PointerType(elementType);
        }

        return weaver.ModuleDefinition.ImportReference(typeDef);
    }

    public static MethodReference ImportMethodReference(this BaseModuleWeaver weaver, Type type, string methodName, params Type[] parameterTypes)
    {
        return weaver.ImportMethodReference(weaver.ImportTypeDefinition(type), methodName, parameterTypes);
    }

    public static MethodReference ImportMethodReference(this BaseModuleWeaver weaver, TypeDefinition typeDef, string methodName, params Type[] parameterTypes)
    {
        if (typeDef == null) return null;
        var methods = typeDef.Methods.Where(m => m.Name == methodName).ToArray();
        foreach (var method in methods)
        {
            if (ParametersMatch(method.Parameters, parameterTypes))
            {
                return weaver.ModuleDefinition.ImportReference(method);
            }
        }
        if (methods.Length == 1)
        {
            return weaver.ModuleDefinition.ImportReference(methods[0]);
        }
        return null;
    }

    public static MethodReference ImportConstructorReference(this BaseModuleWeaver weaver, Type type, params Type[] parameterTypes)
    {
        return weaver.ImportConstructorReference(weaver.ImportTypeDefinition(type), parameterTypes);
    }

    public static MethodReference ImportConstructorReference(this BaseModuleWeaver weaver, TypeDefinition typeDef, params Type[] parameterTypes)
    {
        if (typeDef == null) return null;
        var methods = typeDef.GetConstructors().ToArray();
        foreach (var method in methods)
        {
            if (ParametersMatch(method.Parameters, parameterTypes))
            {
                return weaver.ModuleDefinition.ImportReference(method);
            }
        }
        if (methods.Length == 1)
        {
            return weaver.ModuleDefinition.ImportReference(methods[0]);
        }
        return null;
    }

    public static MethodReference ImportPropertyGetMethodReference(this BaseModuleWeaver weaver, Type type, string propertyName)
    {
        return weaver.ImportPropertyGetMethodReference(weaver.ImportTypeDefinition(type), propertyName);
    }

    public static MethodReference ImportPropertyGetMethodReference(this BaseModuleWeaver weaver, TypeDefinition typeDef, string propertyName)
    {
        if (typeDef == null) return null;
        var method = typeDef.Properties.FirstOrDefault(p => p.Name == propertyName)?.GetMethod;
        return method != null ? weaver.ModuleDefinition.ImportReference(method) : null;
    }

    private static bool ParametersMatch(Collection<ParameterDefinition> parameters, Type[] parameterTypes)
    {
        if (parameters.Count != parameterTypes.Length) return false;
        foreach (var parameter in parameters)
        {
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                // If it's a generic parameter, we can't directly compare it to a Type
                continue;
            }
            else if (paramType.FullName != parameterTypes[parameters.IndexOf(parameter)].FullName)
            {
                return false;
            }
        }
        return true;
    }
}
