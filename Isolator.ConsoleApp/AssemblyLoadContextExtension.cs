public static class AssemblyLoadContextExtension
{
    /// <summary>
    /// Returns a string that represents the context of the assembly in which the specified object is defined.
    /// </summary>
    /// <remarks>This method is an extension method for all objects and delegates to the assembly's context
    /// string representation. The exact format of the returned string depends on the implementation of the assembly's
    /// ToContextString method.</remarks>
    /// <param name="value">The object whose assembly context string is to be retrieved. Cannot be null.</param>
    /// <returns>A string representing the context of the object's assembly.</returns>
    public static string ToContextString(this object value)
    {
        var assembly = value.GetType().Assembly;
        return assembly.ToContextString();
    }

    /// <summary>
    /// Returns a string that represents the context of the assembly in which the specified object is defined.
    /// </summary>
    /// <remarks>This method is an extension method for all objects and delegates to the assembly's context
    /// string representation. The exact format of the returned string depends on the implementation of the assembly's
    /// ToContextString method.</remarks>
    /// <param name="assembly">The assembly whose context string is to be retrieved. Cannot be null.</param>
    /// <returns>A string representing the context of the assembly.</returns>
    public static string ToContextString(this System.Reflection.Assembly assembly)
    {
#if NET
        var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
        return context.Name;
#else
        return "Default";
#endif
    }
}