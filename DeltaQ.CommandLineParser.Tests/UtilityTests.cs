using System;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

using FluentAssertions;
using System.Reflection.Metadata.Ecma335;

namespace DeltaQ.CommandLineParser.Tests
{
	[TestFixture]
	public class UtilityTests
	{
		class GetMemberValueHelper
		{
			public int Field = 42;
			public string Property { get; set; } = "forty two";
			public string ReadOnlyProperty { get; } = "forty three";
		}

		[Test]
		public void GetMemberValue_should_extract_value_from_field()
		{
			// Arrange
			var obj = new GetMemberValueHelper();

			var member = obj.GetType().GetField(nameof(GetMemberValueHelper.Field))!;
			
			// Act
			var result = Utility.GetMemberValue(obj, member);

			// Assert
			result!.Should().Be(42);
		}

		[Test]
		public void GetMemberValue_should_extract_value_from_property()
		{
			// Arrange
			var obj = new GetMemberValueHelper();

			var member = obj.GetType().GetProperty(nameof(GetMemberValueHelper.Property))!;
			
			// Act
			var result = Utility.GetMemberValue(obj, member);

			// Assert
			result!.Should().Be("forty two");
		}

		[Test]
		public void GetMemberValue_should_extract_value_from_readonly_property()
		{
			// Arrange
			var obj = new GetMemberValueHelper();

			var member = obj.GetType().GetProperty(nameof(GetMemberValueHelper.ReadOnlyProperty))!;
			
			// Act
			var result = Utility.GetMemberValue(obj, member);

			// Assert
			result!.Should().Be("forty three");
		}

		[Test]
		public void GetMemberValue_with_type_parameter_should_work()
		{
			// Arrange
			var obj = new GetMemberValueHelper();

			var field = obj.GetType().GetField(nameof(GetMemberValueHelper.Field))!;
			var property = obj.GetType().GetProperty(nameof(GetMemberValueHelper.Property))!;

			// Act
			var result1 = Utility.GetMemberValue<int>(obj, field);
			var result2 = Utility.GetMemberValue<string>(obj, property);

			// Assert
			result1.Should().Be(42);
			result2.Should().Be("forty two");
		}

		[Test]
		public void GetMemberType_should_determine_field_type()
		{
			// Arrange
			var member = typeof(GetMemberValueHelper).GetField(nameof(GetMemberValueHelper.Field))!;
			
			// Act
			var result = Utility.GetMemberType(member);

			// Assert
			result!.Should().Be(typeof(int));
		}

		[Test]
		public void GetMemberType_should_determine_property_type()
		{
			// Arrange
			var member = typeof(GetMemberValueHelper).GetProperty(nameof(GetMemberValueHelper.Property))!;
			
			// Act
			var result = Utility.GetMemberType(member);

			// Assert
			result!.Should().Be(typeof(string));
		}

#pragma warning disable 649
		class FillStructureHelper
		{
			public FillStructureHelperStructure? Structure;
		}

		class FillStructureHelperStructure
		{
			public int Field;
			public bool Skip;
			public string? Property;
		}
#pragma warning restore 649

		[Test]
		public void FillStructure_should_populate_structure()
		{
			// Arrange
			var obj = new FillStructureHelperStructure();

			var argumentAttribute = new ArgumentAttribute();

			argumentAttribute.AttachedToMember = typeof(FillStructureHelper).GetField("Structure");
			argumentAttribute.Properties = new[] { "Field", "Property" };

			var argumentSource = new ArgumentSource(new[] { "42", "forty two" });

			// Act
			Utility.FillStructure(
				obj,
				argumentAttribute,
				argumentSource);

			// Assert
			obj.Field.Should().Be(42);
			obj.Property.Should().Be("forty two");
		}

#pragma warning disable 649
		class GetListElementType_AddToList_Helper
		{
			public System.Collections.ArrayList? ArrayList;
			public System.Collections.Specialized.StringCollection? StringCollection;
			public List<bool>? GenericList;
		}
#pragma warning restore 649

		[Test]
		public void GetListElementType_should_recognize_ArrayList()
		{
			// Act
			Utility.GetListElementType(
				typeof(GetListElementType_AddToList_Helper).GetField(nameof(GetListElementType_AddToList_Helper.ArrayList))!,
				out var listType,
				out var listElementType);

			// Assert
			listType.Should().Be(typeof(System.Collections.ArrayList));
			listElementType.Should().Be(typeof(object));
		}
		
		[Test]
		public void GetListElementType_should_recognize_StringCollection()
		{
			// Act
			Utility.GetListElementType(
				typeof(GetListElementType_AddToList_Helper).GetField(nameof(GetListElementType_AddToList_Helper.StringCollection))!,
				out var listType,
				out var listElementType);

			// Assert
			listType.Should().Be(typeof(System.Collections.Specialized.StringCollection));
			listElementType.Should().Be(typeof(string));
		}
		
		[Test]
		public void GetListElementType_should_recognize_generic_List()
		{
			// Act
			Utility.GetListElementType(
				typeof(GetListElementType_AddToList_Helper).GetField(nameof(GetListElementType_AddToList_Helper.GenericList))!,
				out var listType,
				out var listElementType);

			// Assert
			listType.Should().Be(typeof(ICollection<bool>));
			listElementType.Should().Be(typeof(bool));
		}
		
		[Test]
		public void AddToList_should_work_with_ArrayList()
		{
			// Arrange
			var obj = new GetListElementType_AddToList_Helper();

			var listProperty = obj.GetType().GetField(nameof(GetListElementType_AddToList_Helper.ArrayList))!;

			var itemsToAdd = new object[] { true, 6, 7, "forty two" };

			// Act
			foreach (var item in itemsToAdd)
				Utility.AddToList(obj, listProperty, item);

			// Assert
			obj.ArrayList.Should().NotBeNull();
			obj.ArrayList!.ToArray().Should().BeEquivalentTo(itemsToAdd);
		}
		
		[Test]
		public void AddToList_should_work_with_StringCollection()
		{
			// Arrange
			var obj = new GetListElementType_AddToList_Helper();

			var listProperty = obj.GetType().GetField(nameof(GetListElementType_AddToList_Helper.StringCollection))!;

			var itemsToAdd = new string[] { "true", "6", "7", "forty two" };

			// Act
			foreach (var item in itemsToAdd)
				Utility.AddToList(obj, listProperty, item);

			// Assert
			obj.StringCollection.Should().NotBeNull();
			obj.StringCollection!.Count.Should().Be(itemsToAdd.Length);
			for (int i=0; i < itemsToAdd.Length; i++)
			obj.StringCollection![i].Should().Be(itemsToAdd[i]);
		}
		
		[Test]
		public void AddToList_should_work_with_generic_List()
		{
			// Arrange
			var obj = new GetListElementType_AddToList_Helper();

			var listProperty = obj.GetType().GetField(nameof(GetListElementType_AddToList_Helper.GenericList))!;

			var itemsToAdd = new bool[] { true, false, true, true, false };

			// Act
			foreach (var item in itemsToAdd)
				Utility.AddToList(obj, listProperty, item);

			// Assert
			obj.GenericList.Should().NotBeNull();
			obj.GenericList!.Should().BeEquivalentTo(itemsToAdd);
		}

		[Test]
		public void Coerce_should_return_default_value_type_for_null()
		{
			// Act
			var result = Utility.Coerce(null, typeof(int));

			// Assert
			result.Should().Be(0);
		}

		[Test]
		public void Coerce_should_parse_enum_members()
		{
			// Act
			var result = Utility.Coerce("Read", typeof(FileAccess));

			// Assert
			result.Should().Be(FileAccess.Read);
		}

		[Test]
		public void Coerce_should_parse_numbers()
		{
			// Act
			var result = Utility.Coerce("1.234", typeof(decimal));

			// Assert
			result.Should().Be(1.234M);
		}

		[Test]
		public void Coerce_should_handle_nullable_types()
		{
			// Act
			var result = Utility.Coerce(3, typeof(int?));

			// Assert
			result.Should().Be(3);
		}

		class TryParseHelper
		{
			public static bool ParseCalled = false;

			public static int Parse(string value, out int redHerring)
			{
				throw new Exception("Should not be called");
			}

			public static int Parse(string value)
			{
				ParseCalled = true;
				return 3;
			}
		}

		[TestCase(typeof(byte))]
		[TestCase(typeof(int))]
		[TestCase(typeof(double))]
		[TestCase(typeof(TryParseHelper))]
		public void TryParse_should_dynamically_invoke_parse_method(Type type)
		{
			// Arrange
			object? value = "3";

			// Act
			bool result = Utility.TryParse(ref value, type, typeof(string));

			// Assert
			result.Should().BeTrue();

			value.Should().BeEquivalentTo(3);

			if (!type.IsValueType)
				TryParseHelper.ParseCalled.Should().BeTrue();
		}
	}
}
