using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using NUnit.Framework;

using FluentAssertions;

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

#pragma warning disable 649
		class SwitchesTestClass
		{
			[Switch("/A")] public bool A;
			[Switch("/B")] public int B;
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_include_switches()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<SwitchesTestClass>(bufferWriter, binaryName: "TestBinary");

			// Assert
			bufferWriter.ToString().Should().Be("usage: TestBinary [/A] [/B [/B [..]]\n");
		}

#pragma warning disable 649
		class ArgumentsTestClass
		{
			public struct Structure
			{
				public int X, Y;
			}

			[Argument("/A")] public bool A;
			[Argument("/B", Properties = ["X", "Y"])] public Structure B;
			[Argument("/C")] public List<int> C = new List<int>();
			[Argument("/D", MultipleItemDelimiters = ":")] public List<int> D = new List<int>();
			[Argument("/E", Properties = ["X", "Y"])] public List<Structure> E = new List<Structure>();

			[Argument("/R", IsRemainder = true, Description = "This argument gathers everything remaining on the command-line.")] public string? R;
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_include_arguments()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<ArgumentsTestClass>(bufferWriter, binaryName: "TestBinary");

			// Assert
			bufferWriter.ToString().Should().Be(@"usage: TestBinary [/A <value>] [/B <X> <Y>] [/C <value> [/C <value> [..]]] 
    [/D <value>:<value>:.. [/D <value>:<value>:.. [..]]] [/E <X> <Y> [/E <X> <Y> [..]]] [/R <remainder of command-line>]
");
		}

		class BinaryNameTestClass
		{
		}

		[TestCase("foo")]
		[TestCase("bar")]
		public void ShowUsage_should_use_supplied_binary_name(string binaryName)
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<BinaryNameTestClass>(bufferWriter, binaryName);

			// Assert
			bufferWriter.ToString().Should().Be($"usage: {binaryName}\n");
		}

#pragma warning disable 649
		class FloatingArgumentsTestClass
		{
			public struct Structure
			{
				public int A;
				public double B;
			}

			[Argument(IsFloating = true)]
			public string First = "Yey";

			[Argument(IsFloating = true, Properties = ["A", "B"])]
			public Structure Second;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/")]
			public List<string> Third = new List<string>();
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_format_floating_arguments()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<FloatingArgumentsTestClass>(bufferWriter, binaryName: "TestBinary");

			// Assert
			bufferWriter.ToString().Should().Be(@"usage: TestBinary <First> <Second: A> <Second: B> <Third>/<Third>/..
");
		}

#pragma warning disable 649
		class CombinedTestClass
		{
			[Switch("/A")] public bool A;

			public struct Structure
			{
				public int X, Y;
			}

			[Argument("/C")] public bool C;
			[Argument("/D", Properties = ["X", "Y"])] public Structure D;

			[Switch("/B")] public int B;

			[Argument("/E")] public List<int> E = new List<int>();

			[Argument(IsFloating = true)]
			public string First = "Yey";

			[Argument("/F", MultipleItemDelimiters = ":")] public List<int> F = new List<int>();
			[Argument("/G)", Properties = ["X", "Y"])] public List<Structure> G = new List<Structure>();

			[Argument(IsFloating = true, Properties = ["A", "B"])]
			public Structure Second;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/")]
			public List<string> Third = new List<string>();

			[Argument("/R", IsRemainder = true)] public string? R;
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_format_all_argument_types_combined()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<CombinedTestClass>(bufferWriter, binaryName: "TestBinary");

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary [/A] [/C <value>] [/D <X> <Y>] [/B [/B [..]] [/E <value> [/E <value> [..]]] 
    [/F <value>:<value>:.. [/F <value>:<value>:.. [..]]] [/G) <X> <Y> [/G) <X> <Y> [..]]] <First> <Second: A> 
    <Second: B> <Third>/<Third>/.. [/R <remainder of command-line>]
");
		}

#pragma warning disable 649
		class SwitchesTestClassWithDescriptions
		{
			[Switch("/A")] public bool A;
			[Switch("/B")] public int B;

			[Switch("/C", Description = "Description for the switch for C.")] public bool C;
			[Switch("/D", Description = "And here is a description for the switch for D. Can be specified multiple times.")] public int D;
		}
#pragma warning restore 649


		[Test]
		public void ShowUsage_should_produce_detailed_output_including_switch_descriptions()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<SwitchesTestClassWithDescriptions>(bufferWriter, binaryName: "TestBinary", detailed: true);

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary [/A] [/B [/B [..]] [/C] [/D [/D [..]]

/A

/B

/C                              Description for the switch for C.

/D                              And here is a description for the switch for D. Can be specified multiple times.
");
		}

#pragma warning disable 649
		class ArgumentsTestClassWithDescriptions
		{
			public struct Structure
			{
				public int X, Y;
			}

			[Argument("/A")] public bool A;
			[Argument("/B", Properties = ["X", "Y"])] public Structure B;
			[Argument("/C")] public List<int> C = new List<int>();
			[Argument("/D", MultipleItemDelimiters = ":")] public List<int> D = new List<int>();
			[Argument("/E", Properties = ["X", "Y"])] public List<Structure> E = new List<Structure>();

			[Argument("/F", Description = "And here we have the flag /F, which can be either True or False.")] public bool F;
			[Argument("/G", Properties = ["X", "Y"], Description = "The flag /G requires two arguments to populate X and Y.")] public Structure G;
			[Argument("/H", Description = "The flag /H can be specified multiple times, each time with an integer value.")] public List<int> H = new List<int>();
			[Argument("/I", MultipleItemDelimiters = ":", Description = "The flag /I can receive multiple integer values separated by colon characters (:).")] public List<int> I = new List<int>();
			[Argument("/J", Properties = ["X", "Y"], Description = "The flag /J can be specified multiple times, and each time requires two arguments to populate X and Y.")] public List<Structure> J = new List<Structure>();

			[Argument("/R", IsRemainder = true, Description = "This argument gathers everything remaining on the command-line.")] public string? R;
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_produce_detailed_output_including_argument_descriptions()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<ArgumentsTestClassWithDescriptions>(bufferWriter, binaryName: "TestBinary", detailed: true);

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary [/A <value>] [/B <X> <Y>] [/C <value> [/C <value> [..]]] 
    [/D <value>:<value>:.. [/D <value>:<value>:.. [..]]] [/E <X> <Y> [/E <X> <Y> [..]]] [/F <value>] [/G <X> <Y>] 
    [/H <value> [/H <value> [..]]] [/I <value>:<value>:.. [/I <value>:<value>:.. [..]]] [/J <X> <Y> [/J <X> <Y> [..]]] 
    [/R <remainder of command-line>]

/A <value>

/B <X> <Y>

/C <value> [/C <value> [..]]

/D <value>:<value>:.. [/D <value>:<value>:.. [..]]

/E <X> <Y> [/E <X> <Y> [..]]

/F <value>
                                And here we have the flag /F, which can be either True or False.

/G <X> <Y>
                                The flag /G requires two arguments to populate X and Y.

/H <value> [/H <value> [..]]
                                The flag /H can be specified multiple times, each time with an integer value.

/I <value>:<value>:.. [/I <value>:<value>:.. [..]]
                                The flag /I can receive multiple integer values separated by colon characters (:).

/J <X> <Y> [/J <X> <Y> [..]]
                                The flag /J can be specified multiple times, and each time requires two arguments to 
                                populate X and Y.

/R <remainder of command-line>
                                This argument gathers everything remaining on the command-line.
");
		}

#pragma warning disable 649
		class FloatingArgumentsTestClassWithDescriptions
		{
			public struct Structure
			{
				public int A;
				public double B;
			}

			[Argument(IsFloating = true)]
			public string First = "Yey";

			[Argument(IsFloating = true, Properties = ["A", "B"])]
			public Structure Second;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/")]
			public List<string> Third = new List<string>();

			[Argument(IsFloating = true, Description = "A simple floating string argument.")]
			public string Fourth = "Yey";

			[Argument(IsFloating = true, Properties = ["A", "B"], Description = "A floating argument with two properties.")]
			public Structure Fifth;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/", Description = "A floating argument of list type, using a slash as its item delimeter.")]
			public List<string> Sixth = new List<string>();
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_produce_detailed_output_including_floating_argument_descriptions()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<FloatingArgumentsTestClassWithDescriptions>(bufferWriter, binaryName: "TestBinary", detailed: true);

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary <First> <Second: A> <Second: B> <Third>/<Third>/.. <Fourth> <Fifth: A> <Fifth: B> <Sixth>/<Sixth>/..

<First>

<Second: A> <Second: B>  

<Third>/<Third>/..

<Fourth>
                                A simple floating string argument.

<Fifth: A> <Fifth: B>  
                                A floating argument with two properties.

<Sixth>/<Sixth>/..
                                A floating argument of list type, using a slash as its item delimeter.
");
		}

#pragma warning disable 649
		class CombinedTestClassWithDescriptions
		{
			[Switch("/A")] public bool A;

			public struct Structure
			{
				public int X, Y;
			}

			[Argument("/C")] public bool C;
			[Argument("/D", Properties = ["X", "Y"])] public Structure D;

			[Switch("/B")] public int B;

			[Argument("/E")] public List<int> E = new List<int>();

			[Argument(IsFloating = true)]
			public string First = "Yey";

			[Argument("/F", MultipleItemDelimiters = ":")] public List<int> F = new List<int>();
			[Argument("/G", Properties = ["X", "Y"])] public List<Structure> G = new List<Structure>();

			[Argument(IsFloating = true, Properties = ["A", "B"])]
			public Structure Second;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/")]
			public List<string> Third = new List<string>();

			[Switch("/H", Description = "Description for the switch for H.")] public bool H;

			[Argument("/J", Description = "And here we have the flag /K, which can be either True or False.")] public bool J;

			[Argument("/K", Properties = ["X", "Y"], Description = "The flag /G requires two arguments to populate X and Y.")] public Structure K;

			[Switch("/I", Description = "And here is a description for the switch for I. Can be specified multiple times.")] public int I;

			[Argument("/L", Description = "The flag /H can be specified multiple times, each time with an integer value.")] public List<int> L = new List<int>();

			[Argument(IsFloating = true, Description = "A simple floating string argument.")]
			public string Fourth = "Yey";

			[Argument("/M", MultipleItemDelimiters = ":", Description = "The flag /I can receive multiple integer values separated by colon characters (:).")] public List<int> M = new List<int>();
			[Argument("/N", Properties = ["X", "Y"], Description = "The flag /J can be specified multiple times, and each time requires two arguments to populate X and Y.")] public List<Structure> N = new List<Structure>();

			[Argument(IsFloating = true, Properties = ["A", "B"], Description = "A floating argument with two properties.")]
			public Structure Fifth;

			[Argument(IsFloating = true, MultipleItemDelimiters = "/", Description = "A floating argument of list type, using a slash as its item delimeter.")]
			public List<string> Sixth = new List<string>();

			[Argument("/R", IsRemainder = true, Description = "This argument gathers everything remaining on the command-line.")] public string? R;
		}
#pragma warning restore 649

		[Test]
		public void ShowUsage_should_produce_detailed_output_for_all_argument_types_combined()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<CombinedTestClassWithDescriptions>(bufferWriter, binaryName: "TestBinary", detailed: true);

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary [/A] [/C <value>] [/D <X> <Y>] [/B [/B [..]] [/E <value> [/E <value> [..]]] 
    [/F <value>:<value>:.. [/F <value>:<value>:.. [..]]] [/G <X> <Y> [/G <X> <Y> [..]]] [/H] [/J <value>] [/K <X> <Y>] 
    [/I [/I [..]] [/L <value> [/L <value> [..]]] [/M <value>:<value>:.. [/M <value>:<value>:.. [..]]] 
    [/N <X> <Y> [/N <X> <Y> [..]]] <First> <Second: A> <Second: B> <Third>/<Third>/.. <Fourth> <Fifth: A> <Fifth: B> 
    <Sixth>/<Sixth>/.. [/R <remainder of command-line>]

/A

/C <value>

/D <X> <Y>

/B

/E <value> [/E <value> [..]]

/F <value>:<value>:.. [/F <value>:<value>:.. [..]]

/G <X> <Y> [/G <X> <Y> [..]]

/H                              Description for the switch for H.

/J <value>
                                And here we have the flag /K, which can be either True or False.

/K <X> <Y>
                                The flag /G requires two arguments to populate X and Y.

/I                              And here is a description for the switch for I. Can be specified multiple times.

/L <value> [/L <value> [..]]
                                The flag /H can be specified multiple times, each time with an integer value.

/M <value>:<value>:.. [/M <value>:<value>:.. [..]]
                                The flag /I can receive multiple integer values separated by colon characters (:).

/N <X> <Y> [/N <X> <Y> [..]]
                                The flag /J can be specified multiple times, and each time requires two arguments to 
                                populate X and Y.

/R <remainder of command-line>
                                This argument gathers everything remaining on the command-line.

<First>

<Second: A> <Second: B>  

<Third>/<Third>/..

<Fourth>
                                A simple floating string argument.

<Fifth: A> <Fifth: B>  
                                A floating argument with two properties.

<Sixth>/<Sixth>/..
                                A floating argument of list type, using a slash as its item delimeter.
");
		}

		[Test]
		public void ShowUsage_should_sort_parameters_if_requested()
		{
			// Arrange
			var bufferWriter = new StringWriter();

			var buffer = bufferWriter.GetStringBuilder();

			var sut = new CommandLine();

			// Act
			sut.ShowUsage<CombinedTestClassWithDescriptions>(bufferWriter, binaryName: "TestBinary", detailed: true, sort: true);

			// Assert
			bufferWriter.ToString().Should().Be(
@"usage: TestBinary [/A] [/B [/B [..]] [/C <value>] [/D <X> <Y>] [/E <value> [/E <value> [..]]] 
    [/F <value>:<value>:.. [/F <value>:<value>:.. [..]]] [/G <X> <Y> [/G <X> <Y> [..]]] [/H] [/I [/I [..]] [/J <value>] 
    [/K <X> <Y>] [/L <value> [/L <value> [..]]] [/M <value>:<value>:.. [/M <value>:<value>:.. [..]]] 
    [/N <X> <Y> [/N <X> <Y> [..]]] <First> <Second: A> <Second: B> <Third>/<Third>/.. <Fourth> <Fifth: A> <Fifth: B> 
    <Sixth>/<Sixth>/.. [/R <remainder of command-line>]

/A

/B

/C <value>

/D <X> <Y>

/E <value> [/E <value> [..]]

/F <value>:<value>:.. [/F <value>:<value>:.. [..]]

/G <X> <Y> [/G <X> <Y> [..]]

/H                              Description for the switch for H.

/I                              And here is a description for the switch for I. Can be specified multiple times.

/J <value>
                                And here we have the flag /K, which can be either True or False.

/K <X> <Y>
                                The flag /G requires two arguments to populate X and Y.

/L <value> [/L <value> [..]]
                                The flag /H can be specified multiple times, each time with an integer value.

/M <value>:<value>:.. [/M <value>:<value>:.. [..]]
                                The flag /I can receive multiple integer values separated by colon characters (:).

/N <X> <Y> [/N <X> <Y> [..]]
                                The flag /J can be specified multiple times, and each time requires two arguments to 
                                populate X and Y.

/R <remainder of command-line>
                                This argument gathers everything remaining on the command-line.

<First>

<Second: A> <Second: B>  

<Third>/<Third>/..

<Fourth>
                                A simple floating string argument.

<Fifth: A> <Fifth: B>  
                                A floating argument with two properties.

<Sixth>/<Sixth>/..
                                A floating argument of list type, using a slash as its item delimeter.
");
		}
	}
}
