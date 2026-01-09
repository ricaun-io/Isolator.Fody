using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;

#if !NETSTANDARD
using System.Runtime.Loader;
#endif

internal static class ILTemplate
{
#if !NETSTANDARD
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

            Common.Log("[{0}] InvokeMethod \t {1}.{2}", GetContext().GetContextNumber(), type.Name, method.Name);

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
    internal static void SetContextName(string contextName)
    {
        if (string.IsNullOrWhiteSpace(contextName))
        {
            contextNameDefault = null;
            return;
        }
        var assembly = Assembly.GetExecutingAssembly();
        contextName = contextName
            .Replace("{name}", assembly.GetName().Name)
            .Replace("{guid}", assembly.ManifestModule.ModuleVersionId.ToString())
            .Trim();

        contextNameDefault = $"IsolatorContext.{contextName}";
    }
    private static string GetDefaultContextName()
    {
        if (string.IsNullOrEmpty(contextNameDefault))
        {
            var contextName = GetContextName() ?? "{name}.{guid}";
            SetContextName(contextName);
        }
        return contextNameDefault;
    }

    static ConditionalWeakTable<object, object> _table = new ConditionalWeakTable<object, object>();
    static Dictionary<string, AssemblyLoadContext> _contextTable = new Dictionary<string, AssemblyLoadContext>();
    internal static AssemblyLoadContext GetContext()
    {
        var contextName = GetDefaultContextName();
        lock (_contextTable)
        {
            if (_contextTable.TryGetValue(contextName, out AssemblyLoadContext context))
            {
                return context;
            }

            if (context is null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var location = assembly.Location;

                if (string.IsNullOrEmpty(location) || assembly.IsDynamic)
                {
                    context = AssemblyLoadContext.GetLoadContext(assembly);
                    Common.Log("[{0}] Context.Location.Empty \t '{1}'", context.GetContextNumber(), context.Name);
                    return context;
                }

                context = FindAssemblyLoadContext(contextName);
                if (context is not null)
                {
                    try
                    {
                        ContextInvokeMethod(context, nameof(IsolatorAssemblyLoadContext.AddResolver), location);
                        _contextTable.Add(contextName, context);
                        Common.Log("[{0}] Context.AddResolver \t '{1}'", context.GetContextNumber(), contextName);
                        return context;
                    }
                    catch (Exception ex) { Common.Log("[{0}] Context.AddResolver.Exception \t '{1}'", context.GetContextNumber(), ex); }
                }

                var type = GetIsolatorAssemblyLoadContext();
                context = Activator.CreateInstance(type, contextName, location) as AssemblyLoadContext;
                context?.Unloading += Unloading;

                _contextTable.Add(contextName, context);
                Common.Log("[{0}] Context.CreateInstance \t '{1}'", context.GetContextNumber(), contextName);
                return context;
            }
        }
        return null;
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
        }
    }

    internal static void Unload()
    {
        foreach (var context in _contextTable.Values)
        {
            try
            {
                context.Unloading -= Unloading;
                context.Unload();
                Common.Log("[{0}] Context.Unload \t '{1}'", context.GetContextNumber(), context.Name);
            }
            catch (Exception ex)
            {
                Common.Log("[{0}] Context.Unload.Exception \t '{1}'", context.GetContextNumber(), ex);
            }
        }
        _contextTable.Clear();
    }

    internal static bool IsDefault()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(assembly);

        if (context == AssemblyLoadContext.Default)
            return true;

        return context.Name != GetDefaultContextName();
    }

    internal static void Attach()
    {
        if (IsDefault())
        {
            var context = GetContext();
            Common.Log("[{0}] Context.Attach \t '{1}'", context.GetContextNumber(), context.Name);
        }
    }

    internal class IsolatorAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly List<AssemblyDependencyResolver> _resolvers = new List<AssemblyDependencyResolver>();
        private readonly List<string> _resolverPaths = new List<string>();
        public IsolatorAssemblyLoadContext(string contextName, string assemblyPath) : base(contextName, isCollectible: true)
        {
            // Cannot use 'AddResolver', not supported in the 'AssemblyLoaderImporter' in the 'Isolator.Fody' project.
            _resolvers.Add(new AssemblyDependencyResolver(assemblyPath));
            _resolverPaths.Add(assemblyPath);
        }

        public void AddResolver(string componentAssemblyPath)
        {
            if (string.IsNullOrWhiteSpace(componentAssemblyPath))
                throw new ArgumentException(nameof(componentAssemblyPath));

            if (_resolverPaths.Contains(componentAssemblyPath)) return;

            _resolvers.Add(new AssemblyDependencyResolver(componentAssemblyPath));
            _resolverPaths.Add(componentAssemblyPath);
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

#if NETSTANDARD
    internal static object CreateInstance(object key, params object[] args) { return null; }
    internal static object GetInstance(object key) { return null; }
    internal static object InvokeMethod(object key, string methodName, object[] args, BindingFlags bindingAttr, Type[] methodTypes = null) { return null; }
    internal static void Attach() { }
    internal static bool IsDefault() { return false; }
#endif
}
