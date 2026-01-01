using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fody;

public sealed partial class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif

        if (!IsolatorAvailable()) return;

        var config = new Configuration(Config);

        WriteInfo($"{GetType().Assembly.GetName().Name}.Fody v{GetType().Assembly.GetVersion()}");

        FindMsCoreReferences();
        ImportAssemblyLoader();
        //CallAttach(config);

        IsolatorExecute();
    }

    public bool IsolatorAvailable()
    {
        var systemRuntimeReference = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Runtime");
        if (systemRuntimeReference is not null)
        {
            return systemRuntimeReference.Version.Major >= 6;
        }
        else
        {
            WriteWarning("Could not find a reference to System.Runtime. Isolator.Fody requires .NET 6 or higher.");
            return false;
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
    }

    public override bool ShouldCleanReference => true;
}
