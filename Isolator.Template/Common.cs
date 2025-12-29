using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;


#if NET
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
#endif

internal static class Common
{
#if NET
    internal class LocalAssemblyLoadContext : AssemblyLoadContext
    {
        public bool ConsoleShow { get; set; } = false;
        private void WriteLine(string message)
        {
            if (ConsoleShow)
                Console.WriteLine(message);
        }

        private AssemblyDependencyResolver _resolver;
        private readonly string _assemblyPath;
        private Assembly _assembly;

        public LocalAssemblyLoadContext(string assemblyPath) : base("LocalContext", isCollectible: true)
        {
            this._assemblyPath = assemblyPath;
            this._resolver = new AssemblyDependencyResolver(assemblyPath);

            WriteLine($"{this.Name} Start: {AssemblyName.GetAssemblyName(assemblyPath)}");

            this.Unloading += (context) =>
            {
                WriteLine($"{this.Name} Unload: {context.Name}");
                _assembly = null;
            };
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
            WriteLine($"{this.Name} Load: {name}");

            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
#endif


    //#if NET

    //    //internal static ConditionalWeakTable<object, object> _table = new ConditionalWeakTable<object, object>();

    //    internal static object CreateInstanceInternal(ConditionalWeakTable<object, object> _table, object key, params object[] args)
    //    {
    //        lock (_table)
    //        {
    //            if (_table.TryGetValue(key, out var instance) == false)
    //            {
    //                Console.WriteLine("asdasdsasd");
    //                var context = Get();
    //                var type = key.GetType();
    //                var assembly = context.LoadFromAssemblyName(type.Assembly.GetName());
    //                instance = assembly.CreateInstance(type.FullName, true, BindingFlags.Default, null, args, null, null);
    //                _table.Add(key, instance);
    //            }
    //            return instance;
    //        }
    //    }

    //    public static object GetDataInternal(ConditionalWeakTable<object, object> _table, object key)
    //    {
    //        lock (_table)
    //        {
    //            if (_table.TryGetValue(key, out var instance))
    //            {
    //                return instance;
    //            }
    //            return null;
    //        }
    //    }

    //    static AssemblyLoadContext _context;
    //    internal static AssemblyLoadContext Get()
    //    {
    //        var assembly = Assembly.GetExecutingAssembly();
    //        Console.WriteLine("assembly");
    //        //var context = AssemblyLoadContext.GetLoadContext(assembly);
    //        //Console.WriteLine(context);

    //        //if (IsDefault2() == false)
    //        //    _context = context;

    //        //if (_context is null)
    //        //{
    //        //    _context = new LocalAssemblyLoadContext(assembly.Location);
    //        //    _context.Unloading += Unloading;
    //        //}

    //        return _context;
    //    }

    //    private static void Unloading(AssemblyLoadContext context)
    //    {
    //        _context = null;
    //        Console.WriteLine($"AssemblyLoadContext Unloading: {context.Name}");
    //    }

    //    public static void Unload()
    //    {
    //        _context?.Unload();
    //    }



    //    internal class LocalAssemblyLoadContext : AssemblyLoadContext
    //    {
    //        public bool ConsoleShow { get; set; } = false;
    //        private void WriteLine(string message)
    //        {
    //            if (ConsoleShow)
    //                Console.WriteLine(message);
    //        }

    //        private AssemblyDependencyResolver _resolver;
    //        private readonly string _assemblyPath;
    //        private Assembly _assembly;

    //        public LocalAssemblyLoadContext(string assemblyPath) : base("LocalContext", isCollectible: true)
    //        {
    //            this._assemblyPath = assemblyPath;
    //            this._resolver = new AssemblyDependencyResolver(assemblyPath);

    //            WriteLine($"{this.Name} Start: {AssemblyName.GetAssemblyName(assemblyPath)}");

    //            this.Unloading += (context) =>
    //            {
    //                WriteLine($"{this.Name} Unload: {context.Name}");
    //                _assembly = null;
    //            };
    //        }

    //        public Assembly Initialize()
    //        {
    //            if (_assembly is null)
    //            {
    //                _assembly = LoadFromAssemblyPath(_assemblyPath);
    //            }
    //            return _assembly;
    //        }

    //        protected override Assembly Load(AssemblyName name)
    //        {
    //            WriteLine($"{this.Name} Load: {name}");

    //            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
    //            if (assemblyPath != null)
    //            {
    //                return LoadFromAssemblyPath(assemblyPath);
    //            }

    //            return null;
    //        }
    //    }

    //#else

    //#endif

    //    public static bool IsDefault2()
    //    {
    //#if NET
    //        var assembly = Assembly.GetExecutingAssembly();
    //        var context = AssemblyLoadContext.GetLoadContext(assembly);

    //        if (context == AssemblyLoadContext.Default)
    //            return true;

    //        return context.GetType().Name != nameof(LocalAssemblyLoadContext);
    //#else

    //        return false;

    //#endif
    //}
}
