using System;
using System.Linq;
using System.Reflection;

namespace DQD.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class ArgumentAttribute : ParameterAttribute
	{
		public string? ShortName { get; set; }
		public bool IsRequired { get; set; }
		public bool IsFloating { get; set; }
		public bool IsRemainder { get; set; }
		public string[]? Properties { get; set; }
		public string? MultipleItemDelimiters { get; set; }

		public bool CompleteFiles { get; set; }
		public bool CompleteDirectories { get; set; }
		public string[]? CompleteWith { get; set; }

		internal char[]? DelimiterChars;

		internal string? FloatingArgumentName;

		internal bool HasProperties => (Properties != null) && (Properties.Length > 0);
		internal bool HasDelimiterChars => (DelimiterChars != null) && (DelimiterChars.Length > 0);

		public ArgumentAttribute()
		{
		}

		public ArgumentAttribute(string @switch)
		{
			this.Switch = @switch;
		}

		public ArgumentAttribute(string @switch, params string[] propertyName)
		{
			this.Switch = @switch;
			this.Properties = propertyName;
		}
	}
}

