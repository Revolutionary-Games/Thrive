using System;

/// <summary>
///   Needed to make loading Compounds from json work
/// </summary>
public class EnzymeLoader : BaseThriveConverter
{
    public EnzymeLoader(ISaveContext? context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Enzyme) == objectType;
    }
}
