using System;
using System.Reflection;

namespace DQD.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class BinaryFilePathAttribute : ParameterAttribute
	{
		public BinaryFilePathAttribute()
		{
		}
	}
}
