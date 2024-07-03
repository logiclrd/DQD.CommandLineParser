using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using NUnit.Framework;

using FluentAssertions;

namespace DQD.CommandLineParser.Tests
{
	[TestFixture]
	public class CompletionTests
	{
#pragma warning disable 649
		enum TestEnum
		{
			Fork,
			Knife,
			Spoon,
		}

		enum TestSecondEnum
		{
			Red,
			Green,
			Blue,
		}

		class TestArgs
		{
			[Switch("/FIRST")] public bool First;
			[Switch("/SECOND")] public int Second;
			[Argument("/THIRD")] public string? Third;
			[Argument("/FOURTH")] public int Fourth;
			[Argument("/FIFTH")] public TestEnum Fifth;
		}

		class TestFloatingArgs
		{
			[Argument(IsFloating = true)]
			public TestEnum Float;
		}

		class Structure
		{
			public string? X;
			public TestEnum Y;
			public TestSecondEnum Z;
		}

		class TestPropertiesInArgs
		{
			[Argument("/STRUCT", "X", "Y", "Z")]
			public Structure? Structure;
		}

		class TestFileSystemArgs
		{
			[Argument("/FILE", CompleteFiles = true)]
			public string? File;
			[Argument("/DIRECTORY", CompleteDirectories = true)]
			public string? Directory;
			[Argument("/EITHER", CompleteFiles = true, CompleteDirectories = true)]
			public string? Either;
		}
#pragma warning restore 649

		[TestCase("", new string[] { "/FIRST", "/SECOND", "/THIRD", "/FOURTH", "/FIFTH" })]
		[TestCase("/", new string[] { "/FIRST", "/SECOND", "/THIRD", "/FOURTH", "/FIFTH" })]
		[TestCase("/S", new string[] { "/SECOND" })]
		[TestCase("/F", new string[] { "/FIRST", "/FOURTH", "/FIFTH" })]
		public void Perform_should_enumerate_switches_and_arguments(string prefix, string[] expectedOptions)
		{
			// Arrange
			var configuration = Configuration.Collect(typeof(TestArgs));

			// Act
			var options = Completion.Perform(configuration, prefix, new List<string>()).ToList();

			// Assert
			options.Should().BeEquivalentTo(expectedOptions);
		}

		[TestCase("", new string[] { "Fork", "Knife", "Spoon" })]
		[TestCase("K", new string[] { "Knife" })]
		public void Perform_should_enumerate_options_for_current_argument(string prefix, string[] expectedOptions)
		{
			// Arrange
			var configuration = Configuration.Collect(typeof(TestArgs));

			// Act
			var options = Completion.Perform(configuration, prefix, new List<string>() { "/FIFTH" });

			// Assert
			options.Should().BeEquivalentTo(expectedOptions);
		}

		[TestCase("", new string[] { "Fork", "Knife", "Spoon" })]
		[TestCase("K", new string[] { "Knife" })]
		public void Perform_should_enumerate_options_for_floating_arguments(string prefix, string[] expectedOptions)
		{
			// Arrange
			var configuration = Configuration.Collect(typeof(TestFloatingArgs));

			// Act
			var options = Completion.Perform(configuration, prefix, new List<string>());

			// Assert
			options.Should().BeEquivalentTo(expectedOptions);
		}

		[TestCase(new string[] { "/STRUCT" }, "", new string[0])]
		[TestCase(new string[] { "/STRUCT", "Foo" }, "", new string[] { "Fork", "Knife", "Spoon" })]
		[TestCase(new string[] { "/STRUCT", "Foo" }, "S", new string[] { "Spoon" })]
		[TestCase(new string[] { "/STRUCT", "Foo", "Spoon" }, "", new string[] { "Red", "Green", "Blue" })]
		[TestCase(new string[] { "/STRUCT", "Foo", "Spoon" }, "B", new string[] { "Blue" })]
		public void Perform_should_enumerate_options_for_current_property_in_multi_property_arguments(string[] precedingWords, string prefix, string[] expectedOptions)
		{
			// Arrange
			var configuration = Configuration.Collect(typeof(TestPropertiesInArgs));

			// Act
			var options = Completion.Perform(configuration, prefix, precedingWords.ToList());

			// Assert
			options.Should().BeEquivalentTo(expectedOptions);
		}

		[TestCase("/FILE", "", new string[] { "AFile", "BFile", "BFile2" })]
		[TestCase("/FILE", "A", new string[] { "AFile" })]
		[TestCase("/FILE", "B", new string[] { "BFile", "BFile2" })]
		[TestCase("/DIRECTORY", "", new string[] { "ADirectory", "BDirectory", "BDirectory2" })]
		[TestCase("/DIRECTORY", "A", new string[] { "ADirectory" })]
		[TestCase("/DIRECTORY", "B", new string[] { "BDirectory", "BDirectory2" })]
		[TestCase("/EITHER", "", new string[] { "AFile", "BFile", "BFile2", "ADirectory", "BDirectory", "BDirectory2" })]
		[TestCase("/EITHER", "A", new string[] { "AFile", "ADirectory" })]
		[TestCase("/EITHER", "B", new string[] { "BFile", "BFile2", "BDirectory", "BDirectory2" })]
		public void Perform_should_enumerate_matching_file_system_entries(string argumentSwitch, string prefix, string[] expectedOptions)
		{
			// Arrange
			using (var directory = new TemporaryDirectory())
			using (new CWDScope(directory.Path))
			{
				Directory.CreateDirectory(Path.Combine(directory.Path, "ADirectory"));
				Directory.CreateDirectory(Path.Combine(directory.Path, "BDirectory"));
				Directory.CreateDirectory(Path.Combine(directory.Path, "BDirectory2"));

				File.WriteAllBytes(Path.Combine(directory.Path, "AFile"), Array.Empty<byte>());
				File.WriteAllBytes(Path.Combine(directory.Path, "BFile"), Array.Empty<byte>());
				File.WriteAllBytes(Path.Combine(directory.Path, "BFile2"), Array.Empty<byte>());

				var configuration = Configuration.Collect(typeof(TestFileSystemArgs));

				// Act
				var options = Completion.Perform(configuration, prefix, new List<string>() { argumentSwitch });

				// Assert
				options.Should().BeEquivalentTo(expectedOptions);
			}
		}
	}
}
