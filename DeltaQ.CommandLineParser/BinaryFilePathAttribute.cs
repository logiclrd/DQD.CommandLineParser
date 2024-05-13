using System;
using System.Reflection;

namespace DeltaQ.CommandLineParser
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class BinaryFilePathAttribute : ParameterAttribute
	{
		public BinaryFilePathAttribute()
		{
		}
	}
}
