using System.Linq;
using Mono.Cecil;

internal static class CustomAttributeExtension
{
    public static bool TryGetAndRemoveCustomAttribute(this ICustomAttributeProvider provider, string attributeFullName)
    {
        return provider.TryGetAndRemoveCustomAttribute(attributeFullName, out _);
    }
    public static bool TryGetAndRemoveCustomAttribute(this ICustomAttributeProvider provider, string attributeFullName, out CustomAttribute customAttribute)
    {
        customAttribute = provider.CustomAttributes
            .SingleOrDefault(x => x.AttributeType.FullName == attributeFullName);

        if (customAttribute != null)
        {
            provider.CustomAttributes.Remove(customAttribute);
            return true;
        }

        return false;
    }
}
