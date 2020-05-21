using System;
using Newtonsoft.Json;

/// <summary>
///   Converter for Species
/// </summary>
public class SpeciesConverter : BaseThriveConverter
{
    public SpeciesConverter(ISaveContext context) : base(context)
    {
    }

    /// <summary>
    ///   Valid for Species and subclasses
    /// </summary>
    public override bool CanConvert(Type objectType)
    {
        return typeof(Species).IsAssignableFrom(objectType);
    }
}
