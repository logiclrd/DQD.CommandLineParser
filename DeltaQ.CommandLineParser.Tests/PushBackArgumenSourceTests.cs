using System;

using NUnit.Framework;

using Bogus;

using FluentAssertions;
using System.Collections.Generic;

namespace DeltaQ.CommandLineParser.Tests
{
	[TestFixture]
	public class PushBackArgumentSourceTests
	{
		[Test]
		public void PushBackArgumentSource_should_work_with_empty_remainder()
		{
			// Arrange
			var faker = new Faker();

			var extraArgument = faker.Lorem.Word();
			var remainder = new ArgumentSource(Array.Empty<string>());

			var sut = new PushBackArgumentSource(extraArgument, remainder);

			// Act
			var hasNext1 = sut.HasNext();
			var current1 = sut.Current;
			var peek1 = sut.Peek(0);
			var pull1 = sut.Pull();

			var hasNext2 = sut.HasNext();

			// Assert
			hasNext1.Should().Be(true);
			current1.Should().Be(extraArgument);
			peek1.Should().Be(extraArgument);
			pull1.Should().Be(extraArgument);

			hasNext2.Should().BeFalse();
		}

		[Test]
		public void PushBackArgumentSource_should_work_with_non_empty_remainder()
		{
			// Arrange
			var faker = new Faker();

			var extraArgument = faker.Lorem.Word();
			var remainderArguments = faker.Lorem.Words(20);
			var remainder = new ArgumentSource(remainderArguments);

			var sut = new PushBackArgumentSource(extraArgument, remainder);

			// Act
			var result = new List<string>();

			while (sut.HasNext())
			  result.Add(sut.Pull());

			// Assert
			result.Should().HaveCount(remainderArguments.Length + 1);
			result[0].Should().Be(extraArgument);

			result.RemoveAt(0);

			result.Should().BeEquivalentTo(remainderArguments);
		}
	}
}
