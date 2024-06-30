using System;
using System.IO;
using System.Text;

namespace DQD.CommandLineParser
{
	public class WordWrappingTextWriter : TextWriter
	{
		TextWriter _underlying;
		int _lineWidth;
		int _firstLineIndent;
		int _hangingIndent;

		bool _leaveOpen;

		StringBuilder _wordBuilder;
		int _charsSoFarThisLine;
		bool _startOfParagraph;
		bool _startOfLine;
		bool _startOfWord;
		char _spaceCharacter;

		public override Encoding Encoding => _underlying.Encoding;

		public WordWrappingTextWriter(TextWriter toWrap)
			: this(toWrap, lineWidth: Console.WindowWidth)
		{
		}

		public WordWrappingTextWriter(TextWriter toWrap, int lineWidth)
			: this(toWrap, lineWidth, firstLineIndent: 0, hangingIndent: 0)
		{
		}

		public WordWrappingTextWriter(TextWriter toWrap, int lineWidth, int firstLineIndent, int hangingIndent)
			: this(toWrap, lineWidth, firstLineIndent, hangingIndent, leaveOpen: false)
		{
		}

		public WordWrappingTextWriter(TextWriter toWrap, int lineWidth, int firstLineIndent, int hangingIndent, bool leaveOpen)
		{
			_underlying = toWrap;
			_lineWidth = lineWidth;
			_firstLineIndent = firstLineIndent;
			_hangingIndent = hangingIndent;

			_leaveOpen = leaveOpen;

			_wordBuilder = new StringBuilder();
			_startOfParagraph = true;
			_startOfLine = true;
			_startOfWord = true;
		}

		public int LineWidth
		{
			get => _lineWidth;
			set => _lineWidth = value;
		}

		public int FirstLineIndent
		{
			get => _firstLineIndent;
			set => _firstLineIndent = value;
		}

		public int HangingIndent
		{
			get => _hangingIndent;
			set => _hangingIndent = value;
		}

		bool IsBreakingWhiteSpace(char ch)
		{
			return (ch != '\xA0') && char.IsWhiteSpace(ch);
		}

		void DebugWriteLine(string format, params object?[] args)
		{
			// Console.WriteLine(format, args);
		}

		public override void Write(char ch)
		{
			DebugWriteLine("Write('{0}') // (char){1}", ch, (int)ch);

			if (!IsBreakingWhiteSpace(ch) && (ch != '\0'))
			{
				DebugWriteLine("=> buffer in _wordBuilder");
				_wordBuilder.Append(ch);
				_startOfWord = false;
				_startOfLine = false;
			}
			else if (_startOfWord && (ch != '\n'))
			{
				if (_startOfLine)
					DebugWriteLine("=> is a space character, but still at start of line, doing nothing");
				else
				{
					DebugWriteLine("=> is a space character, but still at start of word, outputting previous space character if any ({0})", (int)_spaceCharacter);

					if (_spaceCharacter != '\0')
					{
						_underlying.Write(_spaceCharacter);

						if (_spaceCharacter == '\t')
							_charsSoFarThisLine = ((_charsSoFarThisLine + 8) / 8) * 8;
						else
							_charsSoFarThisLine++;
					}

					_spaceCharacter = ch;
				}
			}
			else
			{
				if (!_startOfLine && (_spaceCharacter != '\0'))
				{
					_underlying.Write(_spaceCharacter);

					if (_spaceCharacter == '\t')
						_charsSoFarThisLine = ((_charsSoFarThisLine + 8) / 8) * 8;
					else
						_charsSoFarThisLine++;

					_spaceCharacter = '\0';
				}

				FlushWord();

				_startOfWord = true;

				if (ch == '\n')
				{
					_underlying.WriteLine();
					_charsSoFarThisLine = 0;
					_startOfLine = true;
					_startOfParagraph = true;

					_spaceCharacter = '\0';
				}
				else
					_spaceCharacter = ch;
			}
		}

		void FlushWord()
		{
			int remainingChars = _lineWidth - _charsSoFarThisLine;

			if ((_spaceCharacter != '\0') && !_startOfLine)
			{
				_underlying.Write(_spaceCharacter);

				if (_spaceCharacter == '\t')
					_charsSoFarThisLine = ((_charsSoFarThisLine + 8) / 8) * 8;
				else
					_charsSoFarThisLine++;

				_spaceCharacter = '\0';
			}

			if (_wordBuilder.Length + 1 > remainingChars)
			{
				_underlying.WriteLine();
				_charsSoFarThisLine = 0;
				_startOfLine = true;
			}

			if (_startOfParagraph)
			{
				for (int i=0; i < _firstLineIndent; i++)
				{
					_underlying.Write(' ');
					_charsSoFarThisLine++;
				}

				_startOfParagraph = false;
			}
			else if (_startOfLine)
			{
				for (int i=0; i < _hangingIndent; i++)
				{
					_underlying.Write(' ');
					_charsSoFarThisLine++;
				}

				_startOfLine = false;
			}
			else if ((_charsSoFarThisLine > 0) && _startOfWord)
			{
				_underlying.Write(' ');
				_charsSoFarThisLine++;
			}

			_underlying.Write(_wordBuilder);
			_charsSoFarThisLine += _wordBuilder.Length;

			_wordBuilder.Length = 0;

			_startOfWord = false;
		}

		public override void Flush()
		{
			if (_wordBuilder.Length > 0)
				FlushWord();
			else if (_spaceCharacter != '\0')
				Write('\0');
		}

		public override void Close()
		{
			Flush();

			if (!_leaveOpen)
				_underlying.Close();
		}

		protected override void Dispose(bool disposing)
		{
			Close();

			base.Dispose(disposing);

			if (disposing && !_leaveOpen)
				_underlying.Dispose();
		}
	}
}
