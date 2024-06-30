using System;

namespace DQD.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class SwitchAttribute : ParameterAttribute
	{
		internal bool IsCounter;

		public SwitchAttribute()
		{
		}

		public SwitchAttribute(string @switch)
		{
			this.Switch = @switch;
		}
	}
}
