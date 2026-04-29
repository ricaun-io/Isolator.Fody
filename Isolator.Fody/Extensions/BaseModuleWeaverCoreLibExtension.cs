using Fody;
using Mono.Cecil;
using System.Linq;

public static class BaseModuleWeaverCoreLibExtension
{
    public static bool HasCoreLibReference(this ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.AssemblyReferences.Any(ar => ar.Name == "System.Private.CoreLib");
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
