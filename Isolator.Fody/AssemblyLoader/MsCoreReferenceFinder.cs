using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    private MethodReference _compilerGeneratedAttributeCtor;

    private void FindMsCoreReferences()
    {
        if (_compilerGeneratedAttributeCtor != null)
        {
            return;
        }

        var compilerGeneratedAttribute = FindTypeDefinition("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        _compilerGeneratedAttributeCtor = ModuleDefinition.ImportReference(compilerGeneratedAttribute.Methods.First(_ => _.IsConstructor));
    }

    internal void AddCompilerGeneratedAttribute(ICustomAttributeProvider customAttributeProvider)
    {
        if (customAttributeProvider.CustomAttributes.Any(_ => _.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
        {
            return;
        }
        customAttributeProvider.CustomAttributes.Add(new CustomAttribute(_compilerGeneratedAttributeCtor));
    }
}
