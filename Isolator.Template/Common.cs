using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

internal static class Common
{
#if NET

    //[Conditional("DEBUG")]
    //internal static void Log(string message)
    //{
    //    Debug.WriteLine("=== ISOLATOR === " + message);
    //}

    [Conditional("DEBUG")]
    internal static void Log(string format, params object[] args)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
        var contextNumber = GetContextNumber(context);
        Debug.WriteLine($"=== ISOLATOR === [{contextNumber}] => " + string.Format(format, args));
    }

    internal static string GetContextNumber(this System.Runtime.Loader.AssemblyLoadContext context)
    {
        var split = context.ToString().Split('#');
        return split[split.Length - 1];
    }
#endif
}
