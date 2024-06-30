using System;

namespace DQD.CommandLineParser
{
	public class ArgumentSource : IArgumentSource
	{
		string[] _args;
		int _index;

		public ArgumentSource(string[] args)
		{
			_args = args;
			_index = 0;
		}

		public bool HasNext() => HasNext(1);
		public bool HasNext(int count) => (Available >= count);
		public int Available => _args.Length - _index;
		public string? Current => (_index < _args.Length) ? _args[_index] : null;

		public string? Peek(int delta)
		{
			if (delta < 0)
				throw new Exception("Internal error: cannot peek backwards.");

			int offsetIndex = _index + delta;

			return (offsetIndex < _args.Length) ? _args[offsetIndex] : null;
		}

		public string Pull()
		{
			if (!HasNext())
				throw new Exception("Internal error: cannot pull arguments past the end of the command-line.");

			try
			{
				return Current!;
			}
			finally
			{
				_index++;
			}
		}
	}
}

