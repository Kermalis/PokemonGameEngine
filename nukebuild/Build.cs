using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
public sealed partial class Build : NukeBuild
{
    public static readonly AbsolutePath AssetPath = RootDirectory / "PokemonGameEngine" / "Assets";

    public static int Main()
    {
        return Execute<Build>(x => x.Compile);
    }

    private Target Clean => _ => _
    .Executes(CleanWorld, CleanScripts, CleanPokedata);

    private Target Compile => _ => _
    .After(Clean)
    .Executes(BuildWorld, BuildScripts, BuildPokedata);
}