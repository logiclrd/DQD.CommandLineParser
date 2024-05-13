using System;
using System.Collections.Generic;
using System.Linq;

namespace DeltaQ.CommandLineParser
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
	}
}

