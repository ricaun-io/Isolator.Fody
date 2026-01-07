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
#if !NET
    internal static object CreateInstance(object key, params object[] args) { return null; }
    internal static object GetInstance(object key) { return null; }
    internal static object InvokeMethod(object key, string methodName, object[] args, BindingFlags bindingAttr, Type[] methodTypes = null) { return null; }
    internal static void Attach() { }
    internal static bool IsDefault() { return false; }
#endif

#if NET
    internal static object CreateInstance(object key, params object[] args)
    {
        lock (_table)
        {
            if (_table.TryGetValue(key, out var instance) == false)
            {
                var context = GetContext();
                var type = key is Type ? (Type)key : key.GetType();
                var assembly = context.LoadFromAssemblyName(type.Assembly.GetName());
                if (key is Type)
                {
                    instance = assembly.GetType(type.FullName);
                }
                else
                {
                    var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    instance = assembly.CreateInstance(type.FullName, true, bindingAttr, null, args, null, null);
                }
                //Common.Log("[{0}] \t CreateInstance - {1}", context.Name, type.FullName);
                Common.Log("[{0}] CreateInstance \t {1}", context.GetContextNumber(), type.FullName);
                _table.Add(key, instance);
            }
            return instance;
        }
    }
    internal static object InvokeMethod(object key, string methodName, object[] args, BindingFlags bindingAttr, Type[] methodTypes = null)
    {
        var instance = GetInstance(key);
        if (instance != null)
        {
            var type = instance as Type ?? instance.GetType();
            var method = (methodTypes is null) ?
                type.GetMethod(methodName, bindingAttr) :
                type.GetMethod(methodName, bindingAttr, null, methodTypes, null);

            Common.Log("[{0}] InvokeMethod \t {1}", GetContext().GetContextNumber(), method.Name);

            if (method is null)
                throw new MissingMethodException($"Method '{methodName}' not found in type '{type.FullName}'.");

            return method.Invoke(instance is Type ? null : instance, args);
        }
        return null;
    }
    private static object GetInstance(object key)
    {
        lock (_table)
        {
            if (_table.TryGetValue(key, out var instance))
            {
                return instance;
            }
            if (key is Type type)
            {
                return CreateInstance(type, new object[0]);
            }
            return null;
        }
    }

    internal static string GetContextName() => null;
    private static string contextNameDefault = null;
    private static string GetDefaultContextName()
    {
        if (contextNameDefault is null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var contextName = GetContextName() ?? $"{assembly.GetName().Name}.{assembly.ManifestModule.ModuleVersionId.ToString()}";
            contextName = $"IsolatorContext.{contextName}";
            contextNameDefault = contextName;
        }
        return contextNameDefault;
    }

    static ConditionalWeakTable<object, object> _table = new ConditionalWeakTable<object, object>();
    static ConditionalWeakTable<string, AssemblyLoadContext> _contextTable = new ConditionalWeakTable<string, AssemblyLoadContext>();
    //static AssemblyLoadContext _context;
    internal static AssemblyLoadContext GetContext()
    {
        //var context = AssemblyLoadContext.GetLoadContext(assembly);
        var contextName = GetDefaultContextName();
        //Common.Log($"GetContext - {contextName}");
        //Common.Log("[{0}] \t GetContext - {1}", GetContext().Name, contextName);
        AssemblyLoadContext _context = null;
        
        lock (_contextTable)
        {
            if (_contextTable.TryGetValue(contextName, out _context))
            {
                return _context;
            }

            if (_context is null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var location = assembly.Location;

                _context = FindAssemblyLoadContext(contextName);

                if (_context is not null)
                {
                    try
                    {
                        ContextInvokeMethod(_context, nameof(IsolatorAssemblyLoadContext.AddResolver), location);
                        _contextTable.Add(contextName, _context);
                        Common.Log("[{0}] Context.AddResolver \t '{1}'", _context.GetContextNumber(), contextName);
                        return _context;
                    }
                    catch { }
                }

                var type = GetIsolatorAssemblyLoadContext();

                _context = Activator.CreateInstance(type, contextName, location) as AssemblyLoadContext;
                _context?.Unloading += Unloading;


                _contextTable.Add(contextName, _context);
                Common.Log("[{0}] Context.CreateInstance \t '{1}'", _context.GetContextNumber(), contextName);
            }
        }

        return _context;
    }

    private static object ContextInvokeMethod(object instance, string methodName, params object[] parameters)
    {
        var type = instance.GetType();
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method is null)
            throw new MissingMethodException($"Method '{methodName}' not found in type '{type.FullName}'.");

        return method.Invoke(instance, parameters);
    }

    private static Type GetIsolatorAssemblyLoadContext()
    {
        var frame = new System.Diagnostics.StackFrame(0);
        var type = frame.GetMethod().DeclaringType.GetNestedType(nameof(IsolatorAssemblyLoadContext), BindingFlags.Public | BindingFlags.NonPublic);
        return type;
    }

    private static AssemblyLoadContext FindAssemblyLoadContext(string contextName)
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            if (context.Name == contextName)
            {
                var name = context.GetType().Name;
                if (name == nameof(IsolatorAssemblyLoadContext))
                {
                    return context;
                }
            }
        }
        return null;
    }

    private static void Unloading(AssemblyLoadContext context)
    {
        if (_contextTable.Remove(context.Name))
        {
            Common.Log("[{0}] Context.Unloading \t '{1}'", context.GetContextNumber(), context.Name);
            //Common.Log(" \t [{0}] \t Isolator.Unloading", context.Name);
            //_context = null;
            //Console.WriteLine($"Isolator.Unloading ... {context.Name}");
            //Console.WriteLine($"Isolator.Unloading ... {context.ToString()}");
        }
    }

    internal static void Unload()
    {
        //_context?.Unload();
    }

    internal static bool IsDefault()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(assembly);

        if (context == AssemblyLoadContext.Default)
            return true;

        return context.Name != GetDefaultContextName();
        //return context.GetType().Name != nameof(IsolatorAssemblyLoadContext);
    }

    internal static void Attach()
    {
        if (IsDefault()) return;
        var assembly = Assembly.GetExecutingAssembly();
        var context = string.Empty;
#if NET
        context = AssemblyLoadContext.GetLoadContext(assembly).ToString();
#endif
        Console.WriteLine($"Isolator ... {context}");
    }

    internal class IsolatorAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly List<AssemblyDependencyResolver> _resolvers = new List<AssemblyDependencyResolver>();

        public IsolatorAssemblyLoadContext(string contextName, string assemblyPath) : base(contextName, isCollectible: true)
        {
            // Cannot use 'AddResolver', not supported in the 'AssemblyLoaderImporter' in the 'Isolator.Fody' project.
            _resolvers.Add(new AssemblyDependencyResolver(assemblyPath));
        }

        public void AddResolver(string componentAssemblyPath)
        {
            if (string.IsNullOrWhiteSpace(componentAssemblyPath))
                throw new ArgumentException(nameof(componentAssemblyPath));

            _resolvers.Add(new AssemblyDependencyResolver(componentAssemblyPath));
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            foreach (var resolver in _resolvers)
            {
                var path = resolver.ResolveAssemblyToPath(assemblyName);
                if (path != null)
                {
                    return LoadFromAssemblyPath(path);
                }
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (var resolver in _resolvers)
            {
                var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (path != null)
                {
                    return LoadUnmanagedDllFromPath(path);
                }
            }
            return IntPtr.Zero;
        }
    }
#endif

}
