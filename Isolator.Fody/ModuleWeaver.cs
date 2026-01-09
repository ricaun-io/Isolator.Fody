using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fody;

public sealed partial class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        if (!IsolatorAvailable()) return;

        var config = new Configuration(Config);

        WriteInfo($"{GetType().Assembly.GetName().Name} v{GetType().Assembly.GetVersion()}");

        ImportAssemblyLoader(config.EnableDebug);

        FindContextNameMethod(config.ContextName);
        CallAttach(config.LoadAtModuleInit);

        IsolatorExecute();
    }

    public bool IsolatorAvailable()
    {
        var systemRuntimeReference = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Runtime");
        if (systemRuntimeReference is not null && systemRuntimeReference.Version.Major >= 6)
        {
            var config = new Configuration(Config);
            if (config.SkipIsolator)
            {
                IsolatorExecuteRemoveAttributes();
                WriteWarning($"Isolator.Fody is skipped via configuration '{nameof(Configuration.SkipIsolator)}'.");
                return false;
            }

            return true;
        }

        IsolatorExecuteRemoveAttributes();
        WriteWarning("Could not find a reference to System.Runtime. Isolator.Fody requires .NET 6 or higher.");
        return false;
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
    }

    public override bool ShouldCleanReference => true;
}
