using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using NUnit.Framework;

namespace DeltaQ.CommandLineParser.Tests
{
	[TestFixture]
	public class CommandLineTests
	{
		class Dummy
		{
		}

		[Test]
		public void UnrecognizedArgument_handler_should_suppress_exceptions()
		{
			// Arrange
			string? unrecognizedArgumentReceived = null;

			var sut = new CommandLine();

			sut.UnrecognizedArgument +=
				(sender, e) =>
				{
					unrecognizedArgumentReceived = e;
				};

			var args = new string[] { "test", "/UNRECOGNIZED" };

			// Act
			sut.Parse<Dummy>(args);

			// Assert
			unrecognizedArgumentReceived.Should().Be(args[1]);
		}

		[Test]
		public void Parse_should_throw_on_unrecognized_argument_with_no_handler()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new string[] { "test", "/UNRECOGNIZED" };

			// Act & Assert
			Action act = () => sut.Parse<Dummy>(args);

			// Assert
			act.Should().Throw<Exception>();
		}

#pragma warning disable 649
		class ParseHelper
		{
			[BinaryFilePath]
			public string? Binary;

			[Switch] 
			public bool Test;

			[Switch]
			public int Count;
		}
#pragma warning restore 649

		[TestCase(false)]
		[TestCase(true)]
		public void Parse_should_assign_binaryFilePath(bool includeTestSwitch)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");

			if (includeTestSwitch)
				args.Add("/Test");

			// Act
			var result = sut.Parse<ParseHelper>(args);

			// Assert
			result.Binary.Should().Be(args[0]);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Parse_should_set_binary_switches(bool includeTestSwitch)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");

			if (includeTestSwitch)
				args.Add("/Test");

			// Act
			var result = sut.Parse<ParseHelper>(args);

			// Assert
			result.Test.Should().Be(includeTestSwitch);
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		public void Parse_should_set_counter_switches(int count)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");

			for (int i=0; i < count; i++)
				args.Add("/Count");

			// Act
			var result = sut.Parse<ParseHelper>(args);

			// Assert
			result.Count.Should().Be(count);
		}

#pragma warning disable 649
		class ParseArgumentHelper
		{
			[Argument] public int Integer;
			[Argument] public float Float;
			[Argument] public string? String;
		}
#pragma warning restore 649

		[TestCase("Integer")]
		[TestCase("Float")]
		[TestCase("String")]
		public void Parse_should_assign_argument(string fieldName)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add($"/{fieldName}");
			args.Add("3");

			var expectedValue = (fieldName == nameof(ParseArgumentHelper.String)) ? "3" : (object)3;

			// Act
			var result = sut.Parse<ParseArgumentHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.GetType().GetField(fieldName)!.GetValue(result).Should().BeEquivalentTo(expectedValue);
		}

#pragma warning disable 649
		class ParseListHelper
		{
			[Argument]
			public List<string>? Value;
		}
#pragma warning restore 649

		[Test]
		public void Parse_should_add_items_to_list()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add("/Value");
			args.Add("First");
			args.Add("/Value");
			args.Add("Second");
			args.Add("/Value");
			args.Add("Third");

			// Act
			var result = sut.Parse<ParseListHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.Value.Should().NotBeNull();
			result!.Value!.Should().BeEquivalentTo(new[] { "First", "Second", "Third" });
		}

#pragma warning disable 649
		class ParseRemainderHelper
		{
			[Argument] public string? First;
			[Argument(IsRemainder = true)] public string? Second;
			[Argument] public string? Third;
		}
#pragma warning restore 649

		public void Parse_should_aggregate_remainder()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add("/First");
			args.Add("One");
			args.Add("/Second");
			args.Add("Two");
			args.Add("/Third");
			args.Add("Three");
			args.Add("Contains Space");

			// Act
			var result = sut.Parse<ParseRemainderHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.First.Should().Be("One");
			result!.Second.Should().Be("Two /Third Three \"Contains Space\"");
			result!.Third.Should().BeNull();
		}

#pragma warning disable 649
		class ParseFloatingHelper
		{
			[Argument(IsFloating = true)] public string? First;
			[Argument] public string? Second;
			[Argument(IsFloating = true)] public List<string>? Third;
			[Argument] public string? Fourth;
			[Argument(IsFloating = true)] public string? Fifth; // Should not be hit because Third is a list
		}
#pragma warning restore 649

		public void Parse_should_collect_floating_arguments()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add("First floating value");
			args.Add("/Second");
			args.Add("Second argument's value");
			args.Add("First element of Third");
			args.Add("/Fourth");
			args.Add("Fourth argument's value");
			args.Add("Second element of Third");
			args.Add("Third element of Third");

			// Act
			var result = sut.Parse<ParseFloatingHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.First.Should().Be(args[1]);
			result!.Second.Should().Be(args[3]);
			result!.Third.Should().BeEquivalentTo(new[] { args[3], args[6], args[7] });
			result!.Fourth.Should().Be(args[5]);
			result!.Fifth.Should().BeNull();
		}

#pragma warning disable 649
		class ParseRequiredHelper
		{
			[Argument] public string? First;
			[Argument(IsRequired = true)] public string? Second;
		}
#pragma warning restore 649

		[Test]
		public void Parse_should_throw_on_missing_required_arguments()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add("/First");
			args.Add("Value");

			// Act
			Action action = () => sut.Parse<ParseRequiredHelper>(args);

			// Assert
			action.Should().Throw<Exception>().Where(e => e.Message.Contains(nameof(ParseRequiredHelper.Second)));
		}

#pragma warning disable 649
		class ParseRequiredFloatingHelper
		{
			[Argument] public string? First;
			[Argument(IsFloating = true, IsRequired = true)] public string? Second;
		}
#pragma warning restore 649

		[Test]
		public void Parse_should_throw_on_missing_required_floating_arguments()
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add("/First");
			args.Add("Value");

			// Act
			Action action = () => sut.Parse<ParseRequiredFloatingHelper>(args);

			// Assert
			action.Should().Throw<Exception>().Where(e => e.Message.Contains(nameof(ParseRequiredHelper.Second)));
		}

#pragma warning disable 649
		class ParseCaseSensitiveSwitchHelper
		{
			[Switch(IsCaseSensitive = true)] public bool casetest;
			[Switch(IsCaseSensitive = true)] public bool CaseTest;
		}
#pragma warning restore 649

		[TestCase(nameof(ParseCaseSensitiveSwitchHelper.casetest))]
		[TestCase(nameof(ParseCaseSensitiveSwitchHelper.CaseTest))]
		public void Parse_should_handle_case_sensitive_switches(string switchName)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add($"/{switchName}");

			// Act
			var result = sut.Parse<ParseCaseSensitiveSwitchHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.GetType().GetField(switchName)!.GetValue(result).Should().Be(true);
			result!.GetType().GetFields().Where(
				f => (f.Name != switchName) && string.Equals(f.Name, switchName, StringComparison.OrdinalIgnoreCase))
				.Single().GetValue(result).Should().Be(false);
		}

#pragma warning disable 649
		class ParseCaseSensitiveArgumentHelper
		{
			[Argument(IsCaseSensitive = true)] public string? casetest;
			[Argument(IsCaseSensitive = true)] public string? CaseTest;
		}
#pragma warning restore 649

		[TestCase(nameof(ParseCaseSensitiveArgumentHelper.casetest))]
		[TestCase(nameof(ParseCaseSensitiveArgumentHelper.CaseTest))]
		public void Parse_should_handle_case_sensitive_arguments(string argumentSwitch)
		{
			// Arrange
			var sut = new CommandLine();

			var args = new List<string>();

			args.Add("/path/to/binary");
			args.Add($"/{argumentSwitch}");
			args.Add("value");

			// Act
			var result = sut.Parse<ParseCaseSensitiveArgumentHelper>(args);

			// Assert
			result.Should().NotBeNull();
			result!.GetType().GetField(argumentSwitch)!.GetValue(result).Should().Be("value");
			result!.GetType().GetFields().Where(
				f => (f.Name != argumentSwitch) && string.Equals(f.Name, argumentSwitch, StringComparison.OrdinalIgnoreCase))
				.Single().GetValue(result).Should().BeNull();
		}
	}
}
