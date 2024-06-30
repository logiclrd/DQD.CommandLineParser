using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DQD.CommandLineParser
{
	public class CommandLine
	{
		string[] s_args = Environment.GetCommandLineArgs();

		public void ClearArgumentData()
		{
			s_args = Array.Empty<string>();
		}

		public void SetArgumentData(IEnumerable<string> args)
		{
			s_args = args.ToArray();
		}

		public TArgs Parse<TArgs>()
			where TArgs : new()
		{
			return Parse<TArgs>(s_args);
		}

		public event EventHandler<string>? UnrecognizedArgument;

		void OnUnrecognizedArgument(string arg)
		{
			if (UnrecognizedArgument == null)
				throw new Exception("Unrecognized argument: " + arg);

			UnrecognizedArgument(null, arg);
		}

		public TArgs Parse<TArgs>(IEnumerable<string> args)
			where TArgs : new()
		{
			var parsedArgs = new TArgs();

			var configuration = Configuration.Collect(typeof(TArgs));

			IArgumentSource source = new ArgumentSource(args.ToArray());

			string binaryFilePath = source.Pull();

			if (configuration.BinaryFilePath != null)
				Utility.SetMemberValue(parsedArgs, configuration.BinaryFilePath.AttachedToMember!, binaryFilePath);

			int floatingArgumentIndex = 0;

			while (source.HasNext())
			{
				var arg = source.Pull();

				if (configuration.Switches.TryGetValue(arg, out var switchAttribute)
				 || (configuration.Switches.TryGetValue(arg.ToLower(), out switchAttribute) && !switchAttribute.IsCaseSensitive))
				{
					switchAttribute.IsPresent = true;

					if (switchAttribute.IsCounter)
					{
						int currentValue = Utility.GetMemberValue<int>(parsedArgs, switchAttribute.AttachedToMember!);

						Utility.SetMemberValue(parsedArgs, switchAttribute.AttachedToMember!, currentValue + 1);
					}
					else
						Utility.SetMemberValue(parsedArgs, switchAttribute.AttachedToMember!, true);
				}
				else if (configuration.Arguments.TryGetValue(arg, out var argumentAttribute)
							|| (configuration.Arguments.TryGetValue(arg.ToLower(), out argumentAttribute) && !argumentAttribute.IsCaseSensitive))
				{
					argumentAttribute.IsPresent = true;

					if (argumentAttribute.IsListType)
					{
						if (argumentAttribute.HasProperties)
						{
							Utility.GetListElementType(
								argumentAttribute.AttachedToMember!,
								out var listType,
								out var listElementType);

							if ((listType == null) || (listElementType == null))
								throw new Exception($"Internal error: detected {argumentAttribute.AttachedToMemberType} as a list type, but couldn't extract type metadata");

							var structure = Activator.CreateInstance(listElementType)!;

							Utility.FillStructure(structure, argumentAttribute, source);
							Utility.AddToList(parsedArgs, argumentAttribute.AttachedToMember!, structure, listType, listElementType);
						}
						else if (argumentAttribute.HasDelimiterChars)
						{
							foreach (string item in source.Pull()!.Split(argumentAttribute.DelimiterChars, StringSplitOptions.RemoveEmptyEntries))
								Utility.AddToList(parsedArgs, argumentAttribute.AttachedToMember!, item);
						}
						else
							Utility.AddToList(parsedArgs, argumentAttribute.AttachedToMember!, source.Pull()!);
					}
					else if (!argumentAttribute.IsRemainder)
					{
						if (!argumentAttribute.HasProperties)
							Utility.SetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!, source.Pull());
						else
						{
							bool created = false;

							object? structure = Utility.GetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!);

							if (structure == null)
							{
								structure = Activator.CreateInstance(argumentAttribute.AttachedToMemberType)!;
								created = true;
							}

							Utility.FillStructure(structure, argumentAttribute, source);

							if (created || argumentAttribute.AttachedToMemberType.IsValueType)
								Utility.SetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!, structure);
						}
					}
					else
					{
						Utility.SetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!, source.PullRemainder());
					}
				}
				else if (floatingArgumentIndex < configuration.FloatingArguments.Count)
				{
					argumentAttribute = configuration.FloatingArguments[floatingArgumentIndex];

					if (argumentAttribute.IsListType)
					{
						foreach (string item in arg.Split(argumentAttribute.DelimiterChars, StringSplitOptions.RemoveEmptyEntries))
							Utility.AddToList(parsedArgs, argumentAttribute.AttachedToMember!, item);
					}
					else
					{
						floatingArgumentIndex++;

						if (argumentAttribute.HasProperties)
						{
							bool created = false;

							object? structure = Utility.GetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!);

							if (structure == null)
							{
								structure = Activator.CreateInstance(argumentAttribute.AttachedToMemberType)!;
								created = true;

								IArgumentSource propertyValueSource;

								if (argumentAttribute.HasDelimiterChars)
									propertyValueSource = new ArgumentSource(arg.Split(argumentAttribute.DelimiterChars));
								else
									propertyValueSource = new PushBackArgumentSource(arg, source);

								Utility.FillStructure(structure, argumentAttribute, propertyValueSource);

								if (created || argumentAttribute.AttachedToMemberType.IsValueType)
									Utility.SetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!, structure);
							}
						}
						else
							Utility.SetMemberValue(parsedArgs, argumentAttribute.AttachedToMember!, arg);
					}
				}
				else
					OnUnrecognizedArgument(arg);
			}

			List<string>? missing = null;

			foreach (var argumentAttribute in configuration.Arguments.Values)
				if (argumentAttribute.IsRequired && !argumentAttribute.IsPresent)
				{
					missing ??= new List<string>();

					if (missing.Count > 0)
						missing.Add(", ");

					missing.Add(argumentAttribute.Switch!);
				}

			if (floatingArgumentIndex < configuration.FloatingArguments.Count)
			{
				for (int i = floatingArgumentIndex; i < configuration.FloatingArguments.Count; i++)
				{
					if (configuration.FloatingArguments[i].IsRequired)
					{
						missing ??= new List<string>();

						if (missing.Count > 0)
							missing.Add(", ");

						missing.Add(configuration.FloatingArguments[i].ShortName!);
					}
				}
			}

			if (missing != null)
			{
				if (missing.Count > 2)
					missing[missing.Count - 2] = " and ";

				if (missing.Count > 1)
					throw new Exception("Missing values for required command-line switches " + string.Join("", missing.ToArray()) + ".");
				else
					throw new Exception("Missing value for required command-line switch " + missing[0] + ".");
			}

			return parsedArgs;
		}

		public void ShowUsage<TArgs>(bool detailed = false, bool sort = false) => ShowUsage<TArgs>(Console.Error, detailed);

		public void ShowUsage<TArgs>(TextWriter output, bool detailed = false, bool sort = false)
		{
			string binaryName = Environment.GetCommandLineArgs()[0];

			if (string.IsNullOrEmpty(binaryName))
				binaryName = "dotnet " + Assembly.GetCallingAssembly().Location;

			ShowUsage<TArgs>(output, binaryName, detailed, sort);
		}

		public void ShowUsage<TArgs>(TextWriter output, string binaryName, bool detailed = false, bool sort = false)
		{
			var configuration = Configuration.Collect(typeof(TArgs));

			int lineWidth = Console.IsOutputRedirected ? 100 : Console.WindowWidth;

			using (var writer = new WordWrappingTextWriter(output, lineWidth, firstLineIndent: 0, hangingIndent: 4, leaveOpen: true))
			{
				writer.Write("usage: {0}", binaryName);

				void OutputArgument(ArgumentAttribute argument, bool nested)
				{
					writer.Write("{0}", argument.Switch);

					if (!argument.HasProperties)
					{
						if (argument.IsListType && argument.HasDelimiterChars)
							writer.Write("\xA0<value>{0}<value>{0}..", argument.DelimiterChars![0]);
						else if (argument.IsRemainder)
							writer.Write("\xA0<remainder\xA0of\x00A0command-line>");
						else
							writer.Write("\xA0<value>");
					}
					else
					{
						foreach (var property in argument.Properties!)
							writer.Write("\xA0<{0}>", property);
					}

					if (argument.IsListType && !nested)
					{
						writer.Write("\xA0[");
						OutputArgument(argument, nested: true);
						writer.Write("\xA0[..]]");
					}
				}

				ArgumentAttribute? remainder = null;

				var parameters = configuration.AllParameters.OrderBy(p => (p is ArgumentAttribute a) && a.IsRemainder);

				if (sort)
					parameters = parameters.ThenBy(p => (p.Switch ?? "").Replace("-", "").Replace("/", ""));

				foreach (var parameter in parameters)
				{
					if (parameter is SwitchAttribute @switch)
					{
						if (@switch.IsCounter)
							writer.Write(" [{0}\xA0[{0}\xA0[..]]", @switch.Switch);
						else
							writer.Write(" [{0}]", @switch.Switch);
					}
					else if ((parameter is ArgumentAttribute argument) && !argument.IsFloating)
					{
						if (argument.IsRemainder)
							remainder = argument;
						else
						{
							writer.Write(argument.IsRequired ? " " : " [");

							OutputArgument(argument, nested: false);

							if (!argument.IsRequired)
								writer.Write(']');
						}
					}
				}

				foreach (var argument in configuration.FloatingArguments)
				{
					if (!argument.HasProperties)
					{
						if (argument.IsListType)
							writer.Write(" <{0}>{1}<{0}>{1}..", argument.FloatingArgumentName, argument.MultipleItemDelimiters![0]);
						else
							writer.Write(" <" + argument.FloatingArgumentName + ">");
					}
					else
					{
						foreach (string property in argument.Properties!)
							writer.Write(" <{0}:\xA0{1}>", argument.FloatingArgumentName, property);
					}
				}

				if (remainder != null)
				{
					writer.Write(remainder.IsRequired ? " " : " [");

					OutputArgument(remainder, nested: false);

					if (!remainder.IsRequired)
						writer.Write(']');
				}

				writer.WriteLine();

				if (detailed)
				{
					foreach (var parameter in parameters)
					{
						if (parameter is SwitchAttribute @switch)
						{
							writer.HangingIndent = 32;

							writer.WriteLine();
							writer.Write(@switch.Switch);

							if (@switch.Description == null)
								writer.WriteLine();
							else
							{
								if (@switch.Switch!.Length < writer.HangingIndent)
								{
									for (int i = @switch.Switch.Length; i < writer.HangingIndent; i++)
										writer.Write(' ');
								}
								else
								{
									writer.WriteLine();

									for (int i = 0; i < writer.HangingIndent; i++)
										writer.Write(' ');
								}

								writer.WriteLine(@switch.Description);
							}
						}
						else if ((parameter is ArgumentAttribute argument) && !argument.IsFloating)
						{
							writer.WriteLine();
							writer.HangingIndent = 8;

							OutputArgument(argument, nested: false);

							if (argument.Description == null)
								writer.WriteLine();
							else
							{
								writer.Flush();

								writer.FirstLineIndent = 32;
								writer.HangingIndent = 32;

								writer.WriteLine();
								if (argument.IsRequired)
									writer.Write("(Required) ");
								writer.Write(argument.Description);

								writer.WriteLine();

								writer.FirstLineIndent = 0;
							}
						}
					}

					foreach (var argument in configuration.FloatingArguments)
					{
						writer.WriteLine();

						writer.Flush();

						if (!argument.HasProperties)
						{
							if (argument.IsListType)
								writer.Write("<{0}>{1}<{0}>{1}..", argument.FloatingArgumentName, argument.MultipleItemDelimiters![0]);
							else
								writer.Write("<" + argument.FloatingArgumentName + ">");
						}
						else
						{
							foreach (string property in argument.Properties!)
								writer.Write("<" + argument.FloatingArgumentName + ":\xA0" + property + "> ");
						}

						writer.WriteLine();

						writer.FirstLineIndent = 32;

						if (argument.IsRequired)
							writer.Write("(Required) ");

						if (!string.IsNullOrEmpty(argument.Description))
						{
							writer.HangingIndent = 32;

							writer.Write(argument.Description);

							writer.HangingIndent = 8;

							writer.WriteLine();
						}

						writer.FirstLineIndent = 0;
					}
				}
			}
		}
	}
}

