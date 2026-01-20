using Isolator.ConsoleApp;
using System.Reflection;
using System.Runtime.Loader;

namespace Isolator.Fody.Tests
{
    public class AssemblyLoadContextTests
    {
        /// <summary>
        /// This AssemblyLoadContext loads assemblies from their file paths into memory, the location will be empty.
        /// </summary>
        class MemoryAssemblyLoadContext : AssemblyLoadContext
        {
            private AssemblyDependencyResolver _resolver;
            public MemoryAssemblyLoadContext(string componentAssemblyPath) : base(nameof(MemoryAssemblyLoadContext), isCollectible: true)
            {
                _resolver = new AssemblyDependencyResolver(componentAssemblyPath);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var assemblyPath = _resolver?.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
                    return LoadFromStream(stream);
                }
                return null!;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }
                return IntPtr.Zero;
            }

        }

        [Test]
        public void TestAssemblyLoadContext()
        {
            var type = typeof(Class);
            var location = type.Assembly.Location;
            var assemblyName = AssemblyName.GetAssemblyName(location);
            var alc = new MemoryAssemblyLoadContext(location);
            var assembly = alc.LoadFromAssemblyName(assemblyName);

            Console.WriteLine(assembly);
            Assert.IsNotNull(assembly);
            Assert.IsEmpty(assembly.Location);

            var instance = assembly.CreateInstance(type.FullName!);
            Console.WriteLine(instance);
            Assert.IsNotNull(instance);

            var value = instance.GetType().GetMethod(nameof(Class.ContextNumber))!.Invoke(instance, null);
            Console.WriteLine(value);
            Assert.IsNotNull(value);
            Assert.Greater((int)value!, 0);

            alc.Unload();
        }
    }
}