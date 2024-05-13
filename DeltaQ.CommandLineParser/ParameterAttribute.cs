using System;
using System.Reflection;

namespace DeltaQ.CommandLineParser
{
	public abstract class ParameterAttribute : Attribute
	{
		public string? Switch { get; set; }
		public string? Description { get; set; }
		public bool IsCaseSensitive { get; set; }

		internal string EffectiveSwitch => IsCaseSensitive ? Switch! : Switch!.ToLower();

		internal MemberInfo? AttachedToMember;
		internal bool IsListType;
		internal bool IsPresent;

		Type? _attachedToMemberType;

		internal Type AttachedToMemberType
		{
			get
			{
				if (_attachedToMemberType == null)
				{
					if (AttachedToMember is FieldInfo fieldInfo)
						_attachedToMemberType = fieldInfo.FieldType;
					if (AttachedToMember is PropertyInfo propertyInfo)
						_attachedToMemberType = propertyInfo.PropertyType;
				}

				if (_attachedToMemberType == null)
					throw new Exception("Internal error: Unable to determine member type for " + (AttachedToMember?.Name ?? "unknown member"));

				return _attachedToMemberType;
			}
		}

	}
}

