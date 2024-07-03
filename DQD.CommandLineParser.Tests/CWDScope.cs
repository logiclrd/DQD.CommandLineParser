using System;

namespace DQD.CommandLineParser.Tests
{
	public class CWDScope : IDisposable
	{
		string _savedCWD;

		public CWDScope()
		{
			_savedCWD = Environment.CurrentDirectory;
		}

		public CWDScope(string newCWD)
			: this()
		{
			Environment.CurrentDirectory = newCWD;
		}

		public void Dispose()
		{
			try
			{
				Environment.CurrentDirectory = _savedCWD;
			}
			catch {}
		}
	}
}
