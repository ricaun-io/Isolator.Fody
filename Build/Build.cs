using Nuke.Common;
using Nuke.Common.Execution;
using ricaun.Nuke;
using ricaun.Nuke.Components;

class Build : NukeBuild, IPublishPack, ICompileExample, IBeforeCompile, IPrePack
{
    string IHazBeforeCompile.Name => "Isolator.Template*";
    bool IHazBeforeCompile.SignCompile => false;
    string IHazMainProject.MainName => "Isolator.ConsoleApp";
    string IHazExample.Name => "Isolator";
    public static int Main() => Execute<Build>(x => x.From<IPublishPack>().Build);
}
