using System;
using System.IO;

namespace DQD.CommandLineParser.Tests
{
	public class TemporaryDirectory : IDisposable
	{
		string _path;
		bool _isDisposed;

		public string Path => _path;

		public TemporaryDirectory()
		{
			_path = Directory.CreateTempSubdirectory().FullName;
		}

		public void Dispose()
		{
			if (!_isDisposed)
			{
				Directory.Delete(_path, recursive: true);
				_isDisposed = true;
			}
		}
	}
}
