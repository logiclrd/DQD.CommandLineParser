using System;
using System.IO;

using NUnit.Framework;

using FluentAssertions;

namespace DQD.CommandLineParser.Tests
{
	[TestFixture]
	public class WordWrappingTextWriterTests
	{
		[Test]
		public void Write_should_emit_word_on_space()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new WordWrappingTextWriter(bufferWriter);

			// Act & Assert
			sut.Write("test");

			buffer.Length.Should().Be(0);

			sut.Write(' ');

			buffer.ToString().Should().Be("test");
		}

		[TestCase("  ")]
		[TestCase("   ")]
		[TestCase("\t")]
		[TestCase("\t\t")]
		[TestCase("\t \t")]
		public void Write_should_emit_multiple_spaces_between_words(string space)
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new WordWrappingTextWriter(bufferWriter);

			// Act
			sut.Write("test" + space + "test");
			sut.Flush();

			// Assert
			buffer.ToString().Should().Be("test" + space + "test");
		}

		[TestCase(9, "This is \na test \nwith \nmultiple \nwords.")]
		[TestCase(10, "This is a \ntest with \nmultiple \nwords.")]
		[TestCase(11, "This is a \ntest with \nmultiple \nwords.")]
		[TestCase(12, "This is a \ntest with \nmultiple \nwords.")]
		[TestCase(13, "This is a \ntest with \nmultiple \nwords.")]
		[TestCase(14, "This is a \ntest with \nmultiple \nwords.")]
		[TestCase(15, "This is a test \nwith multiple \nwords.")]
		[TestCase(16, "This is a test \nwith multiple \nwords.")]
		[TestCase(17, "This is a test \nwith multiple \nwords.")]
		[TestCase(18, "This is a test \nwith multiple \nwords.")]
		[TestCase(19, "This is a test \nwith multiple \nwords.")]
		[TestCase(20, "This is a test with \nmultiple words.")]
		[TestCase(21, "This is a test with \nmultiple words.")]
		[TestCase(22, "This is a test with \nmultiple words.")]
		[TestCase(23, "This is a test with \nmultiple words.")]
		[TestCase(24, "This is a test with \nmultiple words.")]
		[TestCase(25, "This is a test with \nmultiple words.")]
		[TestCase(26, "This is a test with \nmultiple words.")]
		[TestCase(27, "This is a test with \nmultiple words.")]
		[TestCase(28, "This is a test with \nmultiple words.")]
		[TestCase(29, "This is a test with multiple \nwords.")]
		public void Write_should_word_wrap_before_exceeding_line_length(int lineWidth, string expectedOutput)
		{
			const string TestInput = "This is a test with multiple words.";

			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new WordWrappingTextWriter(bufferWriter, lineWidth);

			// Act
			sut.Write(TestInput);
			sut.Flush();

			// Assert
			buffer.ToString().Should().Be(expectedOutput);
		}

		[TestCase(0, 0, 20, "This is the first \nparagraph with \nmultiple words.\nThis is the second \nparagraph with \nmultiple words.\n\nBlank line before \nthis paragraph.")]
		[TestCase(1, 0, 20, " This is the first \nparagraph with \nmultiple words.\n This is the second \nparagraph with \nmultiple words.\n \n Blank line before \nthis paragraph.")]
		[TestCase(0, 1, 20, "This is the first \n paragraph with \n multiple words.\nThis is the second \n paragraph with \n multiple words.\n\nBlank line before \n this paragraph.")]
		[TestCase(1, 1, 20, " This is the first \n paragraph with \n multiple words.\n This is the second \n paragraph with \n multiple words.\n \n Blank line before \n this paragraph.")]
		[TestCase(4, 0, 20, "    This is the \nfirst paragraph \nwith multiple \nwords.\n    This is the \nsecond paragraph \nwith multiple \nwords.\n    \n    Blank line \nbefore this \nparagraph.")]
		[TestCase(4, 1, 20, "    This is the \n first paragraph \n with multiple \n words.\n    This is the \n second paragraph \n with multiple \n words.\n    \n    Blank line \n before this \n paragraph.")]
		[TestCase(0, 4, 20, "This is the first \n    paragraph with \n    multiple words.\nThis is the second \n    paragraph with \n    multiple words.\n\nBlank line before \n    this paragraph.")]
		[TestCase(1, 4, 20, " This is the first \n    paragraph with \n    multiple words.\n This is the second \n    paragraph with \n    multiple words.\n \n Blank line before \n    this paragraph.")]
		[TestCase(4, 4, 20, "    This is the \n    first paragraph \n    with multiple \n    words.\n    This is the \n    second \n    paragraph with \n    multiple words.\n    \n    Blank line \n    before this \n    paragraph.")]
		public void Write_should_handle_indentation_on_paragraph_and_line_starts(int firstLineIndent, int hangingIndent, int lineWidth, string expectedOutput)
		{
			const string TestInput = "This is the first paragraph with multiple words.\nThis is the second paragraph with multiple words.\n\nBlank line before this paragraph.";

			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new WordWrappingTextWriter(bufferWriter, lineWidth, firstLineIndent, hangingIndent);

			// Act
			sut.Write(TestInput);
			sut.Flush();

			// Assert
			buffer.ToString().Should().Be(expectedOutput);
		}

		[Test]
		public void Single_word_at_start_of_line_should_always_be_start_of_paragraph()
		{
			const string TestInput = "test\ntest\ntest\ntest\n\ntest\n\ntest\n\ntest\n\ntest";
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new WordWrappingTextWriter(bufferWriter, 150, firstLineIndent: 0, hangingIndent: 10);

			// Act
			sut.Write(TestInput);
			sut.Flush();

			// Assert
			buffer.ToString().Should().Be(TestInput);
		}
	}
}
