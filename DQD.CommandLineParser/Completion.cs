using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DQD.CommandLineParser
{
	class Completion
	{
		public static IEnumerable<string> Perform(Configuration configuration, string prefix, List<string> precedingWords)
		{
			ArgumentAttribute? inArgument = null;

			int propertyIndex = 0;
			int floatingArgumentIndex = 0;

			var alreadyHaveSwitch = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

			for (int i=0; i < precedingWords.Count; i++)
			{
				if (configuration.Arguments.TryGetValue(precedingWords[i].ToLower(), out var arg))
				{
					if (arg.Switch != null)
						alreadyHaveSwitch.Add(arg.Switch);

					inArgument = arg;
					i++;

					int argParts = (arg.Properties != null) ? arg.Properties.Length : 1;

					if (i + argParts > precedingWords.Count)
					{
						propertyIndex = precedingWords.Count - i;
						break;
					}

					inArgument = null;
					i += argParts - 1;
				}
				else if (inArgument == null)
					floatingArgumentIndex++;
			}

			if (inArgument != null)
			{
				foreach (var arg in CompleteArgument(inArgument, propertyIndex, prefix))
					yield return arg;
			}
			else
			{
				foreach (var @switch in configuration.Switches)
					if ((@switch.Value.Switch is string switchString)
					 && switchString.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
					 && (!alreadyHaveSwitch.Contains(@switch.Value.Switch) || @switch.Value.IsCounter))
						yield return switchString;
				foreach (var arg in configuration.Arguments)
					if ((arg.Value.Switch is string switchString)
					 && switchString.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
					 && (!alreadyHaveSwitch.Contains(@arg.Value.Switch) || arg.Value.IsListType))
						yield return switchString;

				if (floatingArgumentIndex < configuration.FloatingArguments.Count)
					foreach (var arg in CompleteArgument(configuration.FloatingArguments[floatingArgumentIndex], propertyIndex, prefix))
						yield return arg;
			}
		}

		static IEnumerable<string> CompleteArgument(ArgumentAttribute argument, int propertyIndex, string prefix)
		{
			if (argument.CompleteWith != null)
				return argument.CompleteWith;
			else
			{
				Type attachedToType = argument.AttachedToMemberType;

				if (argument.HasProperties)
				{
					string propertyName = argument.Properties![propertyIndex];

					if (attachedToType.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public) is FieldInfo field)
						attachedToType = field.FieldType;
					else if (attachedToType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public) is PropertyInfo property)
						attachedToType = property.PropertyType;
				}

				if (attachedToType.IsEnum)
					return CompleteEnumPossibilities(attachedToType, prefix);
			}

			return EnumerateFileSystemEntries(prefix, argument.CompleteFiles, argument.CompleteDirectories);
		}

		static IEnumerable<string> CompleteEnumPossibilities(Type enumType, string prefix)
		{
			foreach (var value in Enum.GetValues(enumType))
				if ((value.ToString() is string possibility)
				 && possibility.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
					yield return possibility;
		}

		static IEnumerable<string> EnumerateFileSystemEntries(string prefix, bool completeFiles, bool completeDirectories)
		{
			if (completeDirectories)
			{
				if (".".StartsWith(prefix))
					Console.WriteLine(".");
				if ("..".StartsWith(prefix))
					Console.WriteLine("..");
			}

			int separatorIndex = prefix.LastIndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

			string containerPath = separatorIndex < 0 ? "." : prefix.Substring(0, separatorIndex);

			var directoryInfo = new DirectoryInfo(containerPath);

			if (directoryInfo.Exists)
			{
				var containerPathWithSeparator = prefix.Substring(0, separatorIndex + 1);

				prefix = prefix.Substring(separatorIndex + 1);

				foreach (var item in directoryInfo.EnumerateFileSystemInfos(prefix + "*"))
				{
					bool isDirectory = item.Attributes.HasFlag(FileAttributes.Directory);
					bool isFile = !isDirectory;

					if ((isFile && completeFiles)
					 || (isDirectory && completeDirectories))
					 	yield return containerPathWithSeparator + item.Name;
				}
			}
		}

		public static string GenerateRegistration(ShellType shell, string command, Configuration configuration)
		{
			var registration = new StringWriter();

			if (!(configuration.CompleterArgument is CompleterAttribute completerAttribute))
				throw new Exception("Internal error: Can't find the CompleterAttribute");

			switch (shell)
			{
				case ShellType.PowerShell:
				{
					registration.WriteLine("Register-ArgumentCompleter -CommandName \"{0}\" -Native -ScriptBlock {{", command);
					registration.WriteLine("  param($wordToComplete, $commandAst, $cursorPosition)");
					registration.WriteLine("  {0} --complete \"$wordToComplete\" $($commandAst.ToString().Substring(0, $cursorPosition - $wordToComplete.Length - 1))", command);
					registration.WriteLine("}");

					break;
				}
				case ShellType.Bash:
				{
					var functorName = "_" + Guid.NewGuid().ToString("N");

					registration.WriteLine("{0}()", functorName);
					registration.WriteLine("{");
					registration.WriteLine("  if [ $1 = \"{0}\" ]", command);
					registration.WriteLine("  then");
					registration.WriteLine("    COMPREPLY=()");
					registration.WriteLine("    for option in $({0} --complete \"$2\" \"$3\")", command);
					registration.WriteLine("    do");
					registration.WriteLine("      COMPREPLY+=(\"$option\")");
					registration.WriteLine("    done");
					registration.WriteLine("  fi");
					registration.WriteLine("}");
					registration.WriteLine();
					registration.WriteLine("complete -D -F {0}", functorName);

					break;
				}
				default:
					Console.WriteLine("Don't know how to register completion for shell: {0}", shell);
					break;
			}

			return registration.ToString();
		}
	}
}
