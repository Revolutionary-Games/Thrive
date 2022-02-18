using System;

/// <summary>
///   Needed to make loading Compounds from json work
/// </summary>
public class CompoundLoader : BaseThriveConverter
{
    public CompoundLoader(ISaveContext? context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Compound) == objectType;
    }
}
