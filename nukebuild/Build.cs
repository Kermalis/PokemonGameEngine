using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;

[UnsetVisualStudioEnvironmentVariables]
public sealed partial class Build : NukeBuild
{
	public static readonly AbsolutePath AssetPath = RootDirectory / "PokemonGameEngine" / "Assets";

	public static int Main()
	{
		return Execute<Build>(x => x.Compile);
	}

	public Target Clean => _ => _
	.Executes(CleanWorld, CleanScripts, CleanPokedata);

	public Target Compile => _ => _
	.After(Clean)
	.Executes(BuildWorld, BuildScripts, BuildPokedata);

	public Target CleanWorldOnly => _ => _
	.Executes(CleanWorld);

	public Target CompileWorldOnly => _ => _
	.After(CleanWorldOnly)
	.Executes(BuildWorld);

	public Target CleanScriptsOnly => _ => _
	.Executes(CleanScripts);

	public Target CompileScriptsOnly => _ => _
	.After(CleanScriptsOnly)
	.Executes(BuildScripts);

	public Target CleanPokedataOnly => _ => _
	.Executes(CleanPokedata);

	public Target CompilePokedataOnly => _ => _
	.After(CleanPokedataOnly)
	.Executes(BuildPokedata);
}