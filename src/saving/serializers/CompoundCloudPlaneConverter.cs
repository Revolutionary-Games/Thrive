using System;

namespace Saving.Serializers
{
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
            // Since CompoundCloudPlane is not a C# type, we return false
            return false;
        }

        protected override bool SkipMember(string name)
        {
            return base.SkipMember(name);
        }
    }
}
