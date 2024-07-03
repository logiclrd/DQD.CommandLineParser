using System;

namespace DQD.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field)]
	public class CompleterAttribute : ArgumentAttribute
	{
		public CompleterAttribute(string @switch)
			: base(@switch)
		{
		}
	}
}
