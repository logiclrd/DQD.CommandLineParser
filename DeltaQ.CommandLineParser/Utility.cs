using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeltaQ.CommandLineParser
{
	class Utility
	{
		public static object? GetMemberValue(object _this, MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo fieldInfo)
				return fieldInfo.GetValue(_this);
			if (memberInfo is PropertyInfo propertyInfo)
				return propertyInfo.GetValue(_this, null);

			throw new Exception("Internal error: Trying to call GetMemberValue with a " + memberInfo.GetType().Name);
		}

		public static T? GetMemberValue<T>(object _this, MemberInfo memberInfo)
		{
			return (T?)GetMemberValue(_this, memberInfo);
		}

		public static void SetMemberValue(object _this, MemberInfo memberInfo, object? value)
		{
			var requiredType = GetMemberType(memberInfo);

			value = Coerce(value, requiredType);

			if (memberInfo is FieldInfo fieldInfo)
				fieldInfo.SetValue(_this, value);
			else if (memberInfo is PropertyInfo propertyInfo)
				propertyInfo.SetValue(_this, value);
			else
				throw new Exception("Internal error: Trying to call SetMemberValue with a " + memberInfo.GetType().Name);
		}

		public static Type GetMemberType(MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo fieldInfo)
				return fieldInfo.FieldType;
			if (memberInfo is PropertyInfo propertyInfo)
				return propertyInfo.PropertyType;

			throw new Exception("Internal error: trying to call GetMemberType on a " + memberInfo.GetType().Name);
		}

		public static void FillStructure(object structure, ArgumentAttribute argumentAttribute, IArgumentSource source)
		{
			var structureType = structure.GetType();

			foreach (var propertyName in argumentAttribute.Properties!)
			{
				var memberInfos = structureType.GetMember(propertyName, BindingFlags.Instance | BindingFlags.Public);

				bool found = false;

				foreach (var memberInfo in memberInfos)
				{
					if ((memberInfo is FieldInfo)
					 || (memberInfo is PropertyInfo))
					{
						SetMemberValue(structure, memberInfo, source.Pull());
						found = true;
						break;
					}
				}

				if (!found)
					throw new Exception($"Could not locate field or property '{propertyName}' in type '{structureType.Name}'");
			}
		}

		public static bool GetListElementType(MemberInfo memberInfo, out Type? listType, out Type? listElementType)
		{
			listType = GetMemberType(memberInfo);

			if (listType == typeof(System.Collections.ArrayList))
				listElementType = typeof(object);
			else if (listType == typeof(System.Collections.Specialized.StringCollection))
				listElementType = typeof(string);
			else if (listType.IsGenericType)
			{
				listElementType = null;

				foreach (var interfaceType in listType.GetInterfaces())
				{
					var interfaceTypeDefinition = interfaceType.GetGenericTypeDefinition();

					if (interfaceTypeDefinition == typeof(ICollection<>))
					{
						listType = interfaceType;
						listElementType = interfaceType.GetGenericArguments()[0];
						break;
					}
				}
			}
			else
				listType = listElementType = null;

			return (listType != null);
		}

		public static void AddToList(object _this, MemberInfo memberInfo, object value)
		{
			if (!GetListElementType(memberInfo, out var listType, out var listElementType))
				throw new Exception("Internal error: AddToList called on a non-list type");

			AddToList(_this, memberInfo, value, listType!, listElementType!);
		}

		public static void AddToList(object _this, MemberInfo memberInfo, object? value, Type listType, Type listElementType)
		{
			value = Coerce(value, listElementType);

			object? listObject = GetMemberValue(_this, memberInfo);

			if (listObject == null)
			{
				if (listType.IsInterface)
					listType = typeof(List<>).MakeGenericType(listElementType);

				listObject = Activator.CreateInstance(listType)!;

				SetMemberValue(_this, memberInfo, listObject);
			}

			listType.InvokeMember(
				"Add",
				BindingFlags.InvokeMethod,
				null,
				listObject,
				new object?[] { value });
		}

		public static object? Coerce(object? value, Type requiredType)
		{
			if (requiredType.IsGenericType
			 && (requiredType.GetGenericTypeDefinition() == typeof(Nullable<>)))
				requiredType = requiredType.GetGenericArguments()[0];

			if (value == null)
			{
				if (requiredType.IsValueType)
					value = Activator.CreateInstance(requiredType);
			}
			else if ((value is string stringValue) && requiredType.IsEnum)
				value = Enum.Parse(requiredType, stringValue, true);
			else
			{
				Type valueType = value.GetType();

				if (!requiredType.IsAssignableFrom(valueType))
				{
					if (!TryParse(ref value, requiredType, valueType))
						value = Convert.ChangeType(value, requiredType);
				}
			}

			return value;
		}

		internal static bool TryParse(ref object? value, Type requiredType, Type valueType)
		{
			foreach (var parseMethod in requiredType.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				if (parseMethod.Name != "Parse")
					continue;

				var parameters = parseMethod.GetParameters();

				bool isUsable = true;

				int parameterIndex = -1;
				bool isOptional = false;

				for (int i = 0; i < parameters.Length; i++)
				{
					if (parameters[i].IsOut || parameters[i].IsRetval)
					{
						isUsable = false;
						break;
					}

					if (parameters[i].ParameterType.IsAssignableFrom(valueType))
					{
						if (parameterIndex < 0)
						{
							parameterIndex = i;
							isOptional = parameters[i].IsOptional;
						}
						else if (isOptional && !parameters[i].IsOptional)
						{
							parameterIndex = i;
							isOptional = false;
						}
					}

					if ((parameterIndex != i) && !parameters[i].IsOptional)
					{
						isUsable = false;
						break;
					}
				}

				if (isUsable)
				{
					object?[] parameterValues = new object?[parameters.Length];

					for (int i = 0; i < parameters.Length; i++)
					{
						if (i == parameterIndex)
							parameterValues[i] = value;
						else
							parameterValues[i] = parameters[i]!.DefaultValue;
					}

					value = parseMethod!.Invoke(null, (object[])parameterValues);

					return true;
				}
			}

			return false;
		}
	}
}

