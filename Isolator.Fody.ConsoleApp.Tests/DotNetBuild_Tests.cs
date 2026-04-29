using System.Diagnostics;
using System.Reflection;

#if NET10_0
namespace Isolator.Fody.ConsoleApp.Tests
{
    public class DotNetBuild_Tests
    {
        [TestCase("Isolator.Fody.ConsoleApp")]
        public void Test(string projectName)
        {
            var location = Path.GetDirectoryName(typeof(DotNetBuild_Tests).Assembly.Location)!;
            var csprojPath = Path.Combine(location, "..", "..", "..", "..", projectName, $"{projectName}.csproj");

            if (!File.Exists(csprojPath))
                Assert.Ignore($"Project file not found: {csprojPath}");

            var directorny = Path.Combine(location, $"output_{projectName}");
            if (Directory.Exists(directorny))
                Directory.Delete(directorny, true);

            var configuration = "Release";
#if DEBUG
            configuration = "Debug";
#endif

            var output = DotnetBuild(csprojPath, directorny, configuration);
            Console.WriteLine(output);

            var files = Directory.GetFiles(directorny, $"{projectName}.exe", SearchOption.AllDirectories);
            var fileCount = files.Length;
            Assert.That(fileCount, Is.EqualTo(1));

            var file = files[0];

            Console.WriteLine(file);
            var assembly = TryLoadFrom(file);

            Console.WriteLine("---");
            Console.WriteLine(assembly);
            foreach (var name in assembly.GetReferencedAssemblies())
            {
                Console.WriteLine(name);
                if (name.Name == "System.Private.CoreLib")
                {
                    Assert.Fail("System.Private.CoreLib should not be referenced.");
                }
            }
            Console.WriteLine("---");

            var result = TryRunProcess(file, out var processOutput);
            Console.WriteLine(processOutput);

            Assert.True(result);
        }

        private Assembly TryLoadFrom(string path)
        {
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception)
            {
                if (Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var dllPath = Path.ChangeExtension(path, ".dll");
                    if (File.Exists(dllPath))
                    {
                        return Assembly.LoadFrom(dllPath);
                    }
                }
                throw;
            }
        }

        public static string DotnetBuild(string csprojPath, string outputDirectory, string configuration, bool rebuild = true)
        {
            var arguments = $"build \"{csprojPath}\" --configuration {configuration} -o \"{outputDirectory}\"";
            if (rebuild)
                arguments += " -t:Rebuild";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit(5000);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                output += "\nError:\n" + error;
            }
            return output;
        }

        public static bool TryRunProcess(string filePath, out string output)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                output += "\nError:\n" + error;
            }
            return process.ExitCode == 0;
        }
    }
}
#endif