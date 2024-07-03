using System;

namespace DQD.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RegisterCompleterAttribute : ArgumentAttribute
	{
		public RegisterCompleterAttribute(string @switch)
			: base(@switch)
		{
		}

		public string? CommandName;
	}
}
