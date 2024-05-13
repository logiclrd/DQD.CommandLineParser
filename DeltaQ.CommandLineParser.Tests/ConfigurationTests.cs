using System;

using NUnit.Framework;

using FluentAssertions;

namespace DeltaQ.CommandLineParser.Tests
{
	[TestFixture]
	public class ConfigurationTests
	{
		class Empty { }

		[Test]
		public void Collect_should_work_on_empty_class()
		{
			// Act
			var configuration = Configuration.Collect(typeof(Empty));

			// Assert
			configuration.BinaryFilePath.Should().BeNull();
			configuration.Switches.Should().BeEmpty();
			configuration.Arguments.Should().BeEmpty();
			configuration.FloatingArguments.Should().BeEmpty();
		}

#pragma warning disable 649
		class BinaryPathField { [BinaryFilePath] public string? BinaryField; }
		class BinaryPathProperty { [BinaryFilePath] public string? BinaryProperty { get; set; } }
#pragma warning restore 649

		[TestCase(typeof(BinaryPathField), nameof(BinaryPathField.BinaryField))]
		[TestCase(typeof(BinaryPathProperty), nameof(BinaryPathProperty.BinaryProperty))]
		public void Collect_should_find_binary_file_path_member(Type type, string memberName)
		{
			// Act
			var configuration = Configuration.Collect(type);

			// Assert
			configuration.BinaryFilePath.Should().NotBeNull();
			configuration.BinaryFilePath!.AttachedToMember.Should().NotBeNull();
			configuration.BinaryFilePath!.AttachedToMember!.Name.Should().Be(memberName);
		}

#pragma warning disable 649
		class Switches
		{
			[Switch("/A")] public bool A;
			[Switch("/B")] public int B { get; set; }
		}
#pragma warning restore 649

		[Test]
		public void Collect_should_find_switches()
		{
			// Act
			var configuration = Configuration.Collect(typeof(Switches));

			// Assert
			configuration.Switches.Should().ContainKey("/a");
			configuration.Switches.Should().ContainKey("/b");

			var aSwitch = configuration.Switches["/a"];
			var bSwitch = configuration.Switches["/b"];

			aSwitch.AttachedToMember!.Should().Be(typeof(Switches).GetField("A"));
			bSwitch.AttachedToMember!.Should().Be(typeof(Switches).GetProperty("B"));

			aSwitch.AttachedToMemberType.Should().Be(typeof(bool));
			bSwitch.AttachedToMemberType.Should().Be(typeof(int));
		}

#pragma warning disable 649
		class Arguments
		{
			[Argument("/A")] public string? A;
			[Argument("/B", IsRequired = true)] public int B;
			[Argument("/C", IsRemainder = true)] public string? C { get; set; }
			[Argument("/D", Properties = new[] { "X", "Y" })] public Structure? D;
		}

		class Structure
		{
			public int X;
			public string? Y { get; set; }
		}
#pragma warning restore 649

		[Test]
		public void Collect_should_find_arguments()
		{
			// Act
			var configuration = Configuration.Collect(typeof(Arguments));

			// Assert
			configuration.Arguments.Should().ContainKey("/a");
			configuration.Arguments.Should().ContainKey("/b");
			configuration.Arguments.Should().ContainKey("/c");
			configuration.Arguments.Should().ContainKey("/d");

			var aArgument = configuration.Arguments["/a"];
			var bArgument = configuration.Arguments["/b"];
			var cArgument = configuration.Arguments["/c"];
			var dArgument = configuration.Arguments["/d"];

			aArgument.AttachedToMember!.Should().Be(typeof(Arguments).GetField("A"));
			bArgument.AttachedToMember!.Should().Be(typeof(Arguments).GetField("B"));
			cArgument.AttachedToMember!.Should().Be(typeof(Arguments).GetProperty("C"));
			dArgument.AttachedToMember!.Should().Be(typeof(Arguments).GetField("D"));

			aArgument.AttachedToMemberType.Should().Be(typeof(string));
			bArgument.AttachedToMemberType.Should().Be(typeof(int));
			cArgument.AttachedToMemberType.Should().Be(typeof(string));
			dArgument.AttachedToMemberType.Should().Be(typeof(Structure));

			dArgument.Properties.Should().BeEquivalentTo(new[] { "X", "Y" });
		}
	}
}
