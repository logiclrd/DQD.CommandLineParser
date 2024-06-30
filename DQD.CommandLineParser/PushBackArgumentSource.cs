using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DQD.CommandLineParser
{
	class PushBackArgumentSource : IArgumentSource
	{
		string? _extraArgument;
		IArgumentSource _remainder;

		public PushBackArgumentSource(string extraArgument, IArgumentSource remainder)
		{
			_extraArgument = extraArgument;
			_remainder = remainder;
		}

		public string? Current
		{
			get
			{
				if (_extraArgument != null)
					return _extraArgument;
				else
					return _remainder.Current;
			}
		}

		public bool HasNext()
		{
			if (_extraArgument != null)
				return true;
			else
				return _remainder.HasNext();
		}

		public bool HasNext(int count)
		{
			if (_extraArgument != null)
				return _remainder.HasNext(count - 1);
			else
				return _remainder.HasNext(count);
		}

		public string? Peek(int index)
		{
			if (_extraArgument != null)
			{
				if (index == 0)
					return _extraArgument;
				else
					return _remainder.Peek(index - 1);
			}
			else
				return _remainder.Peek(index);
		}

		public string Pull()
		{
			if (_extraArgument != null)
			{
				try
				{
					return _extraArgument;
				}
				finally
				{
					_extraArgument = null;
				}
			}
			else
				return _remainder.Pull();
		}
	}
}
