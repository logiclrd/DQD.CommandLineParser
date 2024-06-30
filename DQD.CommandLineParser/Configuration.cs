using System;
using System.Collections.Generic;
using System.Reflection;

namespace DQD.CommandLineParser
{
	internal class Configuration
	{
		public BinaryFilePathAttribute? BinaryFilePath;
		public Dictionary<string, SwitchAttribute> Switches = new Dictionary<string, SwitchAttribute>();
		public Dictionary<string, ArgumentAttribute> Arguments = new Dictionary<string, ArgumentAttribute>();
		public List<ArgumentAttribute> FloatingArguments = new List<ArgumentAttribute>();
		public List<ParameterAttribute> AllParameters = new List<ParameterAttribute>();

		public static Configuration Collect(Type type)
		{
			var configuration = new Configuration();

			foreach (var memberInfo in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
			{
				FieldInfo? fieldInfo = memberInfo as FieldInfo;
				PropertyInfo? propertyInfo = memberInfo as PropertyInfo;

				if ((fieldInfo != null) || (propertyInfo != null))
				{
					Type memberType = (fieldInfo?.FieldType ?? propertyInfo?.PropertyType)!;

					object[] attributes = memberInfo.GetCustomAttributes(true);

					foreach (var attribute in attributes)
					{
						if (attribute is BinaryFilePathAttribute binaryFilePathAttribute)
						{
							if (configuration.BinaryFilePath != null)
								throw new Exception("There can only be one [BinaryFilePath].");

							binaryFilePathAttribute.AttachedToMember = memberInfo;

							configuration.BinaryFilePath = binaryFilePathAttribute;
						}
						else if (attribute is ParameterAttribute parameterAttribute)
						{
							configuration.AllParameters.Add(parameterAttribute);

							parameterAttribute.IsListType = IsListType(memberType);
							parameterAttribute.AttachedToMember = memberInfo;

							if (parameterAttribute is SwitchAttribute switchAttribute)
							{
								if (string.IsNullOrEmpty(switchAttribute.Switch))
									switchAttribute.Switch = "/" + memberInfo.Name;

								if (memberType == typeof(int))
									switchAttribute.IsCounter = true;
								else if (memberType != typeof(bool))
									throw new Exception("A [Switch] may only be applied to System.Int32 or System.Boolean fields or properties.");

								configuration.Switches[switchAttribute.EffectiveSwitch] = switchAttribute;
							}

							if (parameterAttribute is ArgumentAttribute argumentAttribute)
							{
								if (string.IsNullOrEmpty(argumentAttribute.Switch) && !argumentAttribute.IsFloating)
									argumentAttribute.Switch = "/" + memberInfo.Name;

								if (argumentAttribute.IsFloating)
									argumentAttribute.FloatingArgumentName = memberInfo.Name;

								if (argumentAttribute.IsRemainder)
								{
									if (argumentAttribute.HasProperties)
										throw new Exception("An [Argument(IsRemainder = true)] cannot specify properties to assign.");

									if (argumentAttribute.IsFloating)
										throw new Exception("The IsRemainder and IsFloating argument attribute options may not be combined.");

									if (memberType != typeof(string))
										throw new Exception("An [Argument(IsRemainder = true)] may only be applied to System.String fields or properties.");
								}

								if (!string.IsNullOrEmpty(argumentAttribute.MultipleItemDelimiters))
								{
									if (!parameterAttribute.IsListType)
										throw new Exception("An [Argument(MultipleItemDelimiters = ...)] may only be applied to fields and properties of list type.");

									if (argumentAttribute.HasProperties)
										throw new Exception("An [Argument] attribute may not combine the MultipleItemDelimiters and Properties specifications.");

									argumentAttribute.DelimiterChars = argumentAttribute.MultipleItemDelimiters.ToCharArray();
								}

								if (!argumentAttribute.IsFloating)
									configuration.Arguments[argumentAttribute.EffectiveSwitch] = argumentAttribute;
								else
								{
									if (argumentAttribute.IsListType)
									{
										if (!argumentAttribute.HasDelimiterChars)
											throw new Exception("A floating argument of list type must specify delimiter characters.");
										if (argumentAttribute.HasProperties)
											throw new Exception("A floating argument of list type may not specify properties to fill.");
									}

									if (!string.IsNullOrEmpty(argumentAttribute.Switch))
										throw new Exception("Cannot specify a switch for a floating argument.");

									if (string.IsNullOrEmpty(argumentAttribute.ShortName))
										argumentAttribute.ShortName = memberInfo.Name;

									configuration.FloatingArguments.Add(argumentAttribute);
								}
							}
						}
					}
				}
			}

			for (int i = 1; i < configuration.FloatingArguments.Count; i++)
				if (configuration.FloatingArguments[i].IsRequired
				 && !configuration.FloatingArguments[i - 1].IsRequired)
					throw new Exception("An optional floating argument may not precede a required floating argument.");

			return configuration;
		}

		static bool IsListType(Type type)
		{
			if ((type == typeof(System.Collections.ArrayList))
			 || (type == typeof(System.Collections.Specialized.StringCollection)))
				return true;

			if (type.IsGenericType)
			{
				foreach (Type interfaceType in type.GetInterfaces())
				{
					if (interfaceType.IsGenericType)
					{
						Type interfaceTypeDefinition = interfaceType.GetGenericTypeDefinition();

						if (interfaceTypeDefinition == typeof(ICollection<>))
							return true;
					}
				}
			}

			return false;
		}
	}
}

