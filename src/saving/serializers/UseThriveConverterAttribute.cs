using System;

/// <summary>
///   Attribute for marking a class compatible with ThriveTypeConverter. Note that just adding this attribute doesn't
///   make a class use the type converter,
///   <c>[TypeConverter("Saving.Serializers.{nameof(CompoundDefinitionStringConverter)}")]</c> or another type
///   converter needs be specified to use it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UseThriveConverterAttribute : Attribute
{
}
