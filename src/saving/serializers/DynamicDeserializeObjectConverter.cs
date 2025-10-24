using System;

/// <summary>
///   A really hacky converter that returns can handle once for object
/// </summary>
public class DynamicDeserializeObjectConverter : BaseThriveConverter
{
    private readonly Type baseObjectType = typeof(object);

    private bool canConvertObject = true;

    public override bool CanConvert(Type objectType)
    {
        if (objectType != baseObjectType)
            return false;

        if (canConvertObject)
        {
            canConvertObject = false;
            return true;
        }

        return false;
    }

    public void ResetConversionCounter()
    {
        canConvertObject = true;
    }
}
