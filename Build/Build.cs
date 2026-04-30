using Nuke.Common;
using Nuke.Common.Execution;
using ricaun.Nuke;
using ricaun.Nuke.Components;

class Build : NukeBuild, IPublishPack, ICompileExample, ICompileBefore, ITest, IPrePack
{
    // Use "dotnet build" instead of "msbuild" to build the solution, "dotnet build" can inject incorrect references when "Cecil" is not used correctly.
    public Build() => this.dotnetBuildOnly();
    string IHazCompileBefore.Name => "Isolator.Template*";
    bool IHazCompileBefore.SignCompile => false;
    string IHazMainProject.MainName => "Isolator.Fody.ConsoleApp";
    string IHazExample.Name => "Isolator";
    public static int Main() => Execute<Build>(x => x.From<IPublishPack>().Build);
}
