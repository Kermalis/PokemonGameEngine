using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;

#pragma warning disable IDE0051 // Remove unused private members
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

    private Target CleanWorldOnly => _ => _
    .Executes(CleanWorld);

    private Target CompileWorldOnly => _ => _
    .After(CleanWorldOnly)
    .Executes(BuildWorld);

    private Target CleanScriptsOnly => _ => _
    .Executes(CleanScripts);

    private Target CompileScriptsOnly => _ => _
    .After(CleanScriptsOnly)
    .Executes(BuildScripts);

    private Target CleanPokedataOnly => _ => _
    .Executes(CleanPokedata);

    private Target CompilePokedataOnly => _ => _
    .After(CleanPokedataOnly)
    .Executes(BuildPokedata);
}
#pragma warning restore IDE0051 // Remove unused private members