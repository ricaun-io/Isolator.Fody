public static class AssemblyLoadContextExtension
{
    /// <summary>
    /// Determines whether the assembly context of the specified object is the default context.
    /// </summary>
    /// <remarks>This method relies on the assembly's context string representation to determine if it is the
    /// default. Passing a null value will result in a NullReferenceException.</remarks>
    /// <param name="value">The object whose assembly context is to be evaluated. Cannot be null.</param>
    /// <returns>true if the object's assembly context is the default context; otherwise, false.</returns>
    public static bool IsContextDefault(this object value)
    {
        var assembly = value.GetType().Assembly;
        return assembly.IsContextDefault();
    }

    /// <summary>
    /// Determines whether the specified assembly is loaded into the default context.
    /// </summary>
    /// <param name="assembly">The assembly to evaluate for its loading context. Cannot be null.</param>
    /// <returns>true if the assembly is loaded into the default context; otherwise, false.</returns>
    public static bool IsContextDefault(this System.Reflection.Assembly assembly)
    {
        return assembly.ToContextString() == "Default";
    }

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

    /// <summary>
    /// Returns a context-specific integer identifier for the assembly of the specified object.
    /// </summary>
    /// <param name="value">The object whose assembly is used to determine the context number. Cannot be null.</param>
    /// <returns>An integer representing the context number associated with the object's assembly.</returns>
    public static int ToContextNumber(this object value)
    {
        var assembly = value.GetType().Assembly;
        return assembly.ToContextNumber();
    }

    /// <summary>
    /// Gets the context number associated with the specified assembly's load context.
    /// </summary>
    /// <remarks>On .NET platforms that support AssemblyLoadContext, this method extracts a numeric identifier
    /// from the load context's string representation. On other platforms, the method always returns 0.</remarks>
    /// <param name="assembly">The assembly for which to retrieve the load context number. Cannot be null.</param>
    /// <returns>An integer representing the context number of the assembly's load context. Returns 0 on platforms where load
    /// contexts are not supported.</returns>
    public static int ToContextNumber(this System.Reflection.Assembly assembly)
    {
#if NET
        var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
        var split = context.ToString().Split('#');
        return int.Parse(split[split.Length - 1]);
#else
        return 0;
#endif
    }
}