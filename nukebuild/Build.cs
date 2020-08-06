using Nuke.Common;
using Nuke.Common.Execution;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
public sealed partial class Build : NukeBuild
{
    public static int Main()
    {
        return Execute<Build>(x => x.Compile);
    }

    private Target Clean => _ => _
    .Executes(CleanWorld, CleanScripts);

    private Target Compile => _ => _
    .After(Clean)
    .Executes(BuildWorld, BuildScripts);
}