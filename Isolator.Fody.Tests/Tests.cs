using Isolator.ConsoleApp;

namespace Isolator.Fody.Tests
{
    public class Tests
    {
        [Test]
        public void Program_Main()
        {
            Program.Main(Array.Empty<string>());
        }

        [Test]
        public void Program_References()
        {
            foreach (var assemblyName in typeof(Program).Assembly.GetReferencedAssemblies())
            {
                Console.WriteLine(assemblyName);
            }
        }

        [Test]
        public void AssemblyLoader_Exists()
        {
            var type = typeof(Program).Assembly.GetType("Isolator.AssemblyLoader");
            Assert.NotNull(type);
        }

        [Test]
        public void AssemblyLoader_Method_Log_Exists()
        {
            var type = typeof(Program).Assembly.GetType("Isolator.AssemblyLoader");
            Assert.NotNull(type);

            var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var method = type.GetMethod("Log", bindingFlags);

            // The 'Log' method is only included in DEBUG builds or when `EnableDebug` is set to true.
#if DEBUG
            Assert.NotNull(method);
#else
            Assert.Null(method);
#endif
        }
    }
}