using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;

#if NET
using System.Runtime.Loader;
#endif

internal static class ILTemplate
{
    internal static ConditionalWeakTable<object, object> _table = new ConditionalWeakTable<object, object>();

    internal static object CreateInstance(object key, params object[] args)
    {
#if NET
        lock (_table)
        {
            if (_table.TryGetValue(key, out var instance) == false)
            {
                var context = Get();
                var type = key.GetType();
                var assembly = context.LoadFromAssemblyName(type.Assembly.GetName());
                instance = assembly.CreateInstance(type.FullName, true, BindingFlags.Default, null, args, null, null);
                _table.Add(key, instance);
            }
            return instance;
        }
#else
        return key;
#endif
    }

#if NET
    static AssemblyLoadContext _context;
    static string ContextName = null;
    internal static AssemblyLoadContext Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(assembly);

        if (IsDefault() == false)
            _context = context;

        if (_context is null)
        {
            //_context = new LocalAssemblyLoadContext(assembly.Location);
            var frame = new System.Diagnostics.StackFrame(0);
            var type = frame.GetMethod().DeclaringType.GetNestedType(nameof(LocalAssemblyLoadContext), BindingFlags.Public | BindingFlags.NonPublic);

            _context = Activator.CreateInstance(type, assembly.Location) as AssemblyLoadContext;
            _context?.Unloading += Unloading;
            ContextName = _context.Name;
        }

        return _context;
    }
    private static void Unloading(AssemblyLoadContext context)
    {
        _context = null;
        Console.WriteLine($"Isolator.Unloading ... {ContextName}");
    }

    public static void Unload()
    {
        _context?.Unload();
    }

#endif

    internal static object GetData(object key)
    {
#if NET
        lock (_table)
        {
            if (_table.TryGetValue(key, out var instance))
            {
                return instance;
            }
            return null;
        }
#else
        return null;
#endif
    }

    public static bool IsDefault()
    {
#if NET
        var assembly = Assembly.GetExecutingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(assembly);

        if (context == AssemblyLoadContext.Default)
            return true;

        return context.GetType().Name != nameof(LocalAssemblyLoadContext);
#else
        return false;
#endif
    }

    public static void Attach(bool subscribe)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var context = string.Empty;
#if NET
        context = AssemblyLoadContext.GetLoadContext(assembly).ToString();
#endif
        Console.WriteLine($"Isolator ... {context}");
    }

#if NET
    internal class LocalAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;
        private readonly string _assemblyPath;
        private Assembly _assembly;

        public LocalAssemblyLoadContext(string assemblyPath) : base("LocalContext", isCollectible: true)
        {
            this._assemblyPath = assemblyPath;
            this._resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        public Assembly Initialize()
        {
            if (_assembly is null)
            {
                _assembly = LoadFromAssemblyPath(_assemblyPath);
            }
            return _assembly;
        }

        protected override Assembly Load(AssemblyName name)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
#endif

}
