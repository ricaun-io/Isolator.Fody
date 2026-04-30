using Fody;
using Mono.Cecil;
using System;
using System.Linq;

public static class BaseModuleWeaverCoreLibExtension
{
    public static bool HasCoreLibReference(this ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.AssemblyReferences.Any(ar => ar.Name == "System.Private.CoreLib");
    }

    [Obsolete("This method is intended to be used for testing purposes only. It forces the import of a reference that may not exist in the assembly, which can cause issues when using `dotnet build`. When using `msbuild` the issue does not occur.")]
    internal static void ForceToImportReferenceIncorrectly(this BaseModuleWeaver weaver)
    {
        // This force to import `System.Private.CoreLib` reference, which is not present in the assembly, but may be added by the compiler when using `dotnet build`.
        var _ = weaver.ModuleDefinition.ImportReference(typeof(object)).Resolve();
    }

    public static void EnsureCoreLibReferenceDoesNotExist(this BaseModuleWeaver weaver, bool errorThrow = false)
    {
        if (weaver.ModuleDefinition.HasCoreLibReference())
        {
            weaver.WriteWarning("The assembly has a reference to `System.Private.CoreLib`, which may cause issues when using `dotnet build`. Consider using `msbuild` instead.");
            foreach (var assemblyReference in weaver.ModuleDefinition.AssemblyReferences)
            {
                weaver.WriteWarning($"Reference: {assemblyReference.FullName}");
            }
            if (errorThrow)
                weaver.WriteError("The assembly has a reference to `System.Private.CoreLib`, which may cause issues when using `dotnet build`. Consider using `msbuild` instead.");
        }
    }
}
