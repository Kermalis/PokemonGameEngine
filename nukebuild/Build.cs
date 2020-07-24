using Nuke.Common;
using Nuke.Common.Execution;
using System.IO;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
public sealed partial class Build : NukeBuild
{
    public static int Main()
    {
        return Execute<Build>(x => x.Compile);
    }

    private Target Clean => _ => _
    .Executes(() =>
    {
        if (File.Exists(ScriptOutputPath))
        {
            File.Delete(ScriptOutputPath);
        }
    });

    private Target Compile => _ => _
    .After(Clean)
    .Executes(BuildScripts);
}