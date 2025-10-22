namespace Components;

using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Allows specifying lights on an entity to use with <see cref="EntityLightSystem"/>
/// </summary>
public struct EntityLight : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Light[]? Lights;

    public bool LightsApplied;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentEntityLight;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(Lights);
    }

    public struct Light : IArchivable
    {
        public const ushort SERIALIZATION_VERSION_INNER = 1;

        public Color Color;
        public Vector3 Position;

        /// <summary>
        ///   Don't touch, internal variable used by <see cref="EntityLightSystem"/>
        /// </summary>
        public OmniLight3D? CreatedLight;

        public float Intensity;
        public float Range;
        public float Attenuation;

        public bool Enabled;

        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_INNER;
        public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.EntityLightConfig;
        public bool CanBeReferencedInArchive => false;

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(Color);
            writer.Write(Position);
            writer.Write(Intensity);
            writer.Write(Range);
            writer.Write(Attenuation);
            writer.Write(Enabled);
        }
    }
}

public static class EntityLightHelpers
{
    public static EntityLight ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > EntityLight.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, EntityLight.SERIALIZATION_VERSION);

        return new EntityLight
        {
            Lights = reader.ReadObjectOrNull<EntityLight.Light[]>(),
        };
    }

    public static void DisableAllLights(this ref EntityLight entityLight)
    {
        entityLight.LightsApplied = false;

        var lights = entityLight.Lights;
        if (lights != null)
        {
            int count = lights.Length;
            for (int i = 0; i < count; ++i)
            {
                lights[i].Enabled = false;
            }
        }
    }
}
