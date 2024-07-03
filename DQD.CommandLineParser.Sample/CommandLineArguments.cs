namespace DQD.CommandLineParser.Sample
{
	public class CommandLineArguments
	{
		[Argument("--inputfile", CompleteFiles = true, Description = "Specifies the path to the input file.", IsRequired = true)]
		public string? InputFileName;
		[Argument("--outputdir", CompleteDirectories = true, Description = "Specifies the path to the output directory.")]
		public string? OutputDirectory = ".";
		[Argument("--transform", IsRequired = true)]
		public TransformType Transform;

		[Completer("--complete")]
		public string? Complete;
		[RegisterCompleter("--registercompleter", CommandName = "DQD.CommandLineParser.Sample")]
		public ShellType RegisterForShell;
	}
}
