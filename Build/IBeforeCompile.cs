using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;
using ricaun.Nuke.Components;
using ricaun.Nuke.Extensions;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IBeforeCompile
/// </summary>
public interface IBeforeCompile : IHazBeforeCompile, ICompile, ISign, IRelease, IHazContent, INukeBuild
{
    /// <summary>
    /// Target BeforeCompile
    /// </summary>
    Target BeforeCompile => _ => _
        .TriggeredBy(Clean)
        .Before(Compile)
        .Executes(() =>
        {
            ReportSummaryProjectNames(GetExampleProjects());
            BuildProjectsAndRelease(GetExampleProjects(), ReleaseCompile, ReleaseCompile, SignCompile);
        });
}

/// <summary>
/// IHazExample
/// </summary>
public interface IHazBeforeCompile : IHazSolution, INukeBuild, IHazRelease, IHazSign
{
    /// <summary>
    /// Folder Release 
    /// </summary>
    [Parameter]
    string Folder => TryGetValue(() => Folder) ?? "Release";

    /// <summary>
    /// Example Project matching a wildcard pattern (*.Example)
    /// </summary>
    [Parameter]
    string Name => TryGetValue(() => Name) ?? "*.Example";

    /// <summary>
    /// ReleaseCompile (default: true)
    /// </summary>
    [Parameter]
    bool ReleaseCompile => TryGetValue<bool?>(() => ReleaseCompile) ?? true;

    /// <summary>
    /// SignCompile (default: true)
    /// </summary>
    [Parameter]
    bool SignCompile => TryGetValue<bool?>(() => SignCompile) ?? true;

    /// <summary>
    /// GetExampleProjects
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Project> GetExampleProjects() => Solution.GetAllProjects(Name);
}
