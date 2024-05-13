using System;
using System.Collections.Generic;

using NUnit.Framework;

using Bogus;

using FluentAssertions;

namespace DeltaQ.CommandLineParser.Tests
{
	[TestFixture]
	public class ArgumentSourceTests
	{
		[Test]
		public void HasNext_should_return_false_on_empty_input()
		{
			// Arrange
			var sut = new ArgumentSource(Array.Empty<string>());

			// Act
			var result = sut.HasNext();

			// Assert
			result.Should().Be(false);
		}

		[TestCase(1, 0)]
		[TestCase(2, 0)]
		[TestCase(2, 1)]
		[TestCase(3, 2)]
		public void HasNext_should_return_true_when_item_is_available(int totalItems, int consumed)
		{
			// Arrange
			var faker = new Faker();

			var args = new string[totalItems];

			for (int i=0; i < totalItems; i++)
				args[i] = faker.Lorem.Word();

			var sut = new ArgumentSource(args);

			// Act
			for (int i=0; i < consumed; i++)
				sut.Pull();

			var result = sut.HasNext();

			// Assert
			result.Should().BeTrue();
		}

		[TestCase(1)]
		[TestCase(3)]
		[TestCase(5)]
		public void HasNext_should_return_false_when_last_item_is_consumed(int totalItems)
		{
			// Arrange
			var faker = new Faker();

			var args = new string[totalItems];

			for (int i=0; i < totalItems; i++)
				args[i] = faker.Lorem.Word();

			var sut = new ArgumentSource(args);

			// Act
			for (int i=0; i < totalItems; i++)
				sut.Pull();

			var result = sut.HasNext();

			// Assert
			result.Should().BeFalse();
		}

		public void Peek_should_return_item_without_consuming_it()
		{
			// Arrange
			string[] items = new[] { "one", "two", "three" };

			var sut = new ArgumentSource(items);

			// Act
			var peek1 = sut.Peek(0);
			var peek1_2 = sut.Peek(1);
			var peek1_3 = sut.Peek(2);
			var pull1 = sut.Pull();
			var peek2 = sut.Peek(0);
			var peek2_3 = sut.Peek(1);
			var pull2 = sut.Pull();
			var peek3 = sut.Peek(0);
			var pull3 = sut.Pull();

			// Assert
			peek1.Should().Be(items[0]);
			peek1.Should().Be(pull1);
			peek2.Should().Be(items[1]);
			peek1_2.Should().Be(items[1]);
			peek2.Should().Be(pull2);
			peek3.Should().Be(items[2]);
			peek2_3.Should().Be(items[2]);
			peek1_3.Should().Be(items[2]);
			peek3.Should().Be(pull3);
		}

		[Test]
		public void Pull_should_extract_items_in_order()
		{
			// Arrange
			const int TotalItems = 20;

			var faker = new Faker();

			var args = new string[TotalItems];

			for (int i=0; i < TotalItems; i++)
				args[i] = faker.Lorem.Word();

			var sut = new ArgumentSource(args);

			var pulled = new List<string>();

			// Act
			for (int i=0; i < TotalItems; i++)
				pulled.Add(sut.Pull());

			// Assert
			pulled.Should().BeEquivalentTo(args);
		}
	}
}
