using System;

/// <summary>
///   Specific type node converted
/// </summary>
/// <typeparam name="T">The type of node to convert (and also subtypes)</typeparam>
public class NodeConverter<T> : BaseNodeConverter
{
    public NodeConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }
}
