using System;
using System.IO;
using System.Security.Cryptography;

namespace DQD.CommandLineParser.Sample
{
	public class Program
	{
		public static void Main()
		{
			var parser = new CommandLine();

			var args = parser.Parse<CommandLineArguments>();

			string outputFileName = Path.GetFileNameWithoutExtension(args.InputFileName) + ".transformed" + Path.GetExtension(args.InputFileName);

			using (var input = new StreamReader(args.InputFileName!))
			using (var output = new StreamWriter(Path.Combine(args.OutputDirectory!, outputFileName)))
			{
				while (true)
				{
					int readResult = input.Read();

					if (readResult < 0)
						break;

					char ch = (char)readResult;

					switch (args.Transform)
					{
						case TransformType.UpperCase:
							ch = char.ToUpper(ch);
							break;
						case TransformType.LowerCase:
							ch = char.ToLower(ch);
							break;
						case TransformType.ROT13:
							if (((ch >= 'A') && (ch <= 'M'))
							 || ((ch >= 'a') && (ch <= 'm')))
							  ch = (char)(ch + 13);
							else if (((ch >= 'N') && (ch <= 'Z'))
							      || ((ch >= 'z') && (ch <= 'z')))
							  ch = (char)(ch - 13);
							break;
					}
					
					output.Write(ch);
				}
			}
		}
	}
}
