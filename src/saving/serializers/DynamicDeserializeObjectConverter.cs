using System;

/// <summary>
///   A really hacky converter that returns can handle once for object
/// </summary>
public class DynamicDeserializeObjectConverter : BaseThriveConverter
{
    private bool canConvertObject = true;

    public DynamicDeserializeObjectConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(object) && canConvertObject)
        {
            canConvertObject = false;
            return true;
        }

        return false;
    }
}
