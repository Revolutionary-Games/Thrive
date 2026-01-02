using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   See FloatingChunk for what many of the fields here do
/// </summary>
public struct ChunkConfiguration : IEquatable<ChunkConfiguration>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public string Name;

    /// <summary>
    ///   Possible models / scenes to use for this chunk
    /// </summary>
    public List<ChunkScene> Meshes;

    /// <summary>
    ///   This is the spawn density of the chunk
    /// </summary>
    public float Density;

    public bool Dissolves;
    public float Radius;
    public float ChunkScale;
    public float PhysicsDensity;

    // TODO: rename Size to EngulfSize after making sure it isn't used for other purposes
    public float Size;

    /// <summary>
    ///   Amount of compound vented per second
    /// </summary>
    public float VentAmount;

    /// <summary>
    ///   If > 0 the amount of damage to deal on touch
    /// </summary>
    public float Damages;

    public bool DeleteOnTouch;

    /// <summary>
    ///   The name of the kind of damage type this chunk inflicts.
    /// </summary>
    public string? DamageType;

    public Dictionary<Compound, ChunkCompound>? Compounds;

    /// <summary>
    ///   Whether this chunk type is an Easter egg.
    /// </summary>
    public bool EasterEgg;

    /// <summary>
    ///   The type of enzyme needed to break down this chunk.
    /// </summary>
    public string? DissolverEnzyme;

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ChunkConfiguration;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static bool operator ==(ChunkConfiguration left, ChunkConfiguration right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChunkConfiguration left, ChunkConfiguration right)
    {
        return !(left == right);
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ChunkConfiguration)
            throw new NotSupportedException();

        writer.WriteObject((ChunkConfiguration)obj);
    }

    public static ChunkConfiguration ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new ChunkConfiguration
        {
            Name = reader.ReadString() ?? throw new NullArchiveObjectException(),
            Meshes = reader.ReadObject<List<ChunkScene>>(),
            Density = reader.ReadFloat(),
            Dissolves = reader.ReadBool(),
            Radius = reader.ReadFloat(),
            ChunkScale = reader.ReadFloat(),
            PhysicsDensity = reader.ReadFloat(),
            Size = reader.ReadFloat(),
            VentAmount = reader.ReadFloat(),
            Damages = reader.ReadFloat(),
            DeleteOnTouch = reader.ReadBool(),
            DamageType = reader.ReadString(),
            Compounds = reader.ReadObjectOrNull<Dictionary<Compound, ChunkCompound>>(),
            EasterEgg = reader.ReadBool(),
            DissolverEnzyme = reader.ReadString(),
        };
    }

    public static object ReadFromArchiveBoxed(ISArchiveReader reader, ushort version, int referenceId)
    {
        return ReadFromArchive(reader, version);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Name);
        writer.WriteObject(Meshes);
        writer.Write(Density);
        writer.Write(Dissolves);
        writer.Write(Radius);
        writer.Write(ChunkScale);
        writer.Write(PhysicsDensity);
        writer.Write(Size);
        writer.Write(VentAmount);
        writer.Write(Damages);
        writer.Write(DeleteOnTouch);
        writer.Write(DamageType);

        if (Compounds != null)
        {
            writer.WriteObject(Compounds);
        }
        else
        {
            writer.WriteNullObject();
        }

        writer.Write(EasterEgg);
        writer.Write(DissolverEnzyme);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ChunkConfiguration other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool Equals(ChunkConfiguration other)
    {
        return Name == other.Name &&
            Density == other.Density &&
            Dissolves == other.Dissolves &&
            Radius == other.Radius &&
            ChunkScale == other.ChunkScale &&
            PhysicsDensity == other.PhysicsDensity &&
            Size == other.Size &&
            VentAmount == other.VentAmount &&
            Damages == other.Damages &&
            DeleteOnTouch == other.DeleteOnTouch &&
            Meshes.Equals(other.Meshes) &&
            EasterEgg == other.EasterEgg &&
            DamageType == other.DamageType &&
            DissolverEnzyme == other.DissolverEnzyme &&
            Equals(Compounds, other.Compounds);
    }

    public struct ChunkCompound : IEquatable<ChunkCompound>, IArchivable
    {
        public const ushort SERIALIZATION_VERSION_COMPOUND = 1;

        public float Amount;

        [JsonIgnore]
        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_COMPOUND;

        [JsonIgnore]
        public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ChunkCompound;

        [JsonIgnore]
        public bool CanBeReferencedInArchive => false;

        public static bool operator ==(ChunkCompound left, ChunkCompound right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkCompound left, ChunkCompound right)
        {
            return !(left == right);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            if (type != (ArchiveObjectType)ThriveArchiveObjectType.ChunkCompound)
                throw new NotSupportedException();

            writer.WriteObject((ChunkCompound)obj);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static ChunkCompound ReadFromArchive(ISArchiveReader reader, ushort version)
        {
            if (version is > SERIALIZATION_VERSION_COMPOUND or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_COMPOUND);

            return new ChunkCompound
            {
                Amount = reader.ReadFloat(),
            };
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static object ReadFromArchiveBoxed(ISArchiveReader reader, ushort version, int referenceId)
        {
            return ReadFromArchive(reader, version);
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(Amount);
        }

        public override bool Equals(object? obj)
        {
            if (obj is ChunkCompound other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)Amount;
        }

        public bool Equals(ChunkCompound other)
        {
            return Amount == other.Amount;
        }
    }

    /// <summary>
    ///   Don't modify instances of this class. This is not disposable as this data is loaded from JSON (and archives).
    /// </summary>
#pragma warning disable CA1001
    public class ChunkScene : IArchivable
#pragma warning restore CA1001
    {
        public const ushort SERIALIZATION_VERSION_SCENE = 2;

        /// <summary>
        ///   Scene to use for this chunk. Note that this and the following 2 variables reflect
        ///   <see cref="SceneWithModelInfo"/> but this isn't converted to use that for save compatibility (and this
        ///   would be a slight variant with the scene path not being a loaded scene).
        /// </summary>
        public string ScenePath;

        /// <summary>
        ///   Path to the MeshInstance inside the ScenePath scene, null if it is the root
        /// </summary>
        public NodePath? SceneModelPath;

        /// <summary>
        ///   Path to the AnimationPlayer inside the ScenePath scene, null if no animation
        /// </summary>
        public NodePath? SceneAnimationPath;

        /// <summary>
        ///   Path to the convex collision shape of this chunk's graphical mesh (if any).
        /// </summary>
        public string? ConvexShapePath;

        /// <summary>
        ///   Configuration for complex collision shapes (if any).
        /// </summary>
        public List<ComplexCollisionShapeConfiguration>? ComplexCollisionShapeConfigurations;

        /// <summary>
        ///   Need to be set to true on particle type visuals as those need special handling
        /// </summary>
        public bool IsParticles;

        /// <summary>
        ///   If true, animations won't be stopped on this scene when this is spawned as a chunk
        /// </summary>
        public bool PlayAnimation;

        /// <summary>
        ///   If true, then the default shader (material retrieve) is not done, and it is assumed that normal shader
        ///   operations like dissolving are unavailable
        /// </summary>
        public bool MissingDefaultShaderSupport;

        public ChunkScene(LoadedSceneWithModelInfo fromModelInfo)
        {
            // TODO: investigate if it would make sense to switch ScenePath to be also a loaded scene (that would be
            // saved and loaded from JSON)
            ScenePath = fromModelInfo.LoadedScene.ResourcePath;

            SceneModelPath = fromModelInfo.ModelPath;

            SceneAnimationPath = fromModelInfo.AnimationPlayerPath;

            // Default init for non-copied things
            ConvexShapePath = null;
            ComplexCollisionShapeConfigurations = null;
            IsParticles = false;
            PlayAnimation = false;
            MissingDefaultShaderSupport = false;
        }

        [JsonConstructor]
        public ChunkScene(string scenePath)
        {
            ScenePath = scenePath;

            SceneModelPath = null;
            SceneAnimationPath = null;
            ConvexShapePath = null;
            ComplexCollisionShapeConfigurations = null;
            IsParticles = false;
            PlayAnimation = false;
            MissingDefaultShaderSupport = false;
        }

        [JsonIgnore]
        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_SCENE;

        [JsonIgnore]
        public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ChunkScene;

        [JsonIgnore]
        public bool CanBeReferencedInArchive => false;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            if (type != (ArchiveObjectType)ThriveArchiveObjectType.ChunkScene)
                throw new NotSupportedException();

            writer.WriteObject((ChunkScene)obj);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
        {
            if (version is > SERIALIZATION_VERSION_SCENE or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_SCENE);

            var instance = new ChunkScene(reader.ReadString() ?? throw new NullArchiveObjectException());

            var rawModel = reader.ReadString();
            var rawAnimation = reader.ReadString();

            if (rawModel != null)
                instance.SceneModelPath = new NodePath(rawModel);

            if (rawAnimation != null)
                instance.SceneAnimationPath = new NodePath(rawAnimation);

            instance.ConvexShapePath = reader.ReadString();

            if (version <= 1)
            {
                instance.ComplexCollisionShapeConfigurations = null;
            }
            else
            {
                instance.ComplexCollisionShapeConfigurations =
                    reader.ReadObjectOrNull<List<ComplexCollisionShapeConfiguration>>();
            }

            instance.IsParticles = reader.ReadBool();
            instance.PlayAnimation = reader.ReadBool();
            instance.MissingDefaultShaderSupport = reader.ReadBool();

            return instance;
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(ScenePath);
            writer.Write(SceneModelPath?.ToString());
            writer.Write(SceneAnimationPath?.ToString());
            writer.Write(ConvexShapePath);
            writer.WriteObjectOrNull(ComplexCollisionShapeConfigurations);
            writer.Write(IsParticles);
            writer.Write(PlayAnimation);
            writer.Write(MissingDefaultShaderSupport);
        }
    }

    public class ComplexCollisionShapeConfiguration : IArchivable
    {
        public const ushort SERIALIZATION_VERSION_SCENE = 1;

        /// <summary>
        ///   Path to the convex collision shape of this chunk's graphical mesh (if any).
        /// </summary>
        public string? CollisionShapePath;

        /// <summary>
        ///   Starting position of the shapes. Used with primitive shapes to position them correctly.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        ///   Rotation of the shapes in radians. Used with primitive shapes to roatate them correctly.
        /// </summary>
        public Vector3 Rotation;

        public ComplexCollisionShapeConfiguration(string collisionShapePath, Vector3? position, Vector3? rotation)
        {
            CollisionShapePath = collisionShapePath;
            Position = position ?? Vector3.Zero;
            Rotation = rotation ?? Vector3.Zero;
        }

        [JsonIgnore]
        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_SCENE;

        [JsonIgnore]
        public ArchiveObjectType ArchiveObjectType =>
            (ArchiveObjectType)ThriveArchiveObjectType.ComplexCollisionShapeConfiguration;

        [JsonIgnore]
        public bool CanBeReferencedInArchive => false;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            if (type != (ArchiveObjectType)ThriveArchiveObjectType.ComplexCollisionShapeConfiguration)
                throw new NotSupportedException();

            writer.WriteObject((ComplexCollisionShapeConfiguration)obj);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
        {
            if (version is > SERIALIZATION_VERSION_SCENE or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_SCENE);

            return new ComplexCollisionShapeConfiguration(reader.ReadString() ?? throw new NullArchiveObjectException(),
                reader.ReadVector3(),
                reader.ReadVector3());
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(CollisionShapePath);
            writer.Write(Position);
            writer.Write(Rotation);
        }
    }
}
