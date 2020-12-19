using System;

/// <summary>
///   Converter for CompoundCloudPlane
/// </summary>
public class CompoundCloudPlaneConverter : BaseThriveConverter
{
    public CompoundCloudPlaneConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(CompoundCloudPlane);
    }

    protected override bool SkipMember(string name)
    {
        if (name == "Mesh")
            return true;

        return base.SkipMember(name);
    }
}
