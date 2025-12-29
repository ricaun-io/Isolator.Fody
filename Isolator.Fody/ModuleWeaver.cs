using System.Collections.Generic;
using System.Diagnostics;
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

        var config = new Configuration(Config);

        WriteInfo($"{GetType().Assembly.GetName().Name}.Fody v{GetType().Assembly.GetVersion()}");

        FindMsCoreReferences();
        ImportAssemblyLoader(config.CreateTemporaryAssemblies);
        CallAttach(config);
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
    }

    public override bool ShouldCleanReference => true;
}
