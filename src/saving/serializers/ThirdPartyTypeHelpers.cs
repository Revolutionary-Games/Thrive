namespace Saving.Serializers;

using System;
using System.Reflection;
using SharedBase.Archive;
using Xoshiro.PRNG64;

public static class ThirdPartyTypeHelpers
{
    private const int XOSHIRO_SERIALIZATION_VERSION = 1;

    private static XoshiroStarSerializer? serializer1;

    public static void WriteXoShiRo256StarStar(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.XoShiRo256StarStar)
            throw new NotSupportedException();

        // This assignment should be thread safe even though we shouldn't be using these from multiple threads at once
        serializer1 ??= new XoshiroStarSerializer();

        serializer1.Write(writer, (XoShiRo256starstar)obj);
    }

    public static XoShiRo256starstar ReadXoShiRo256StarStarFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        serializer1 ??= new XoshiroStarSerializer();
        return serializer1.Read(reader, version);
    }

    private class XoshiroStarSerializer
    {
        private readonly FieldInfo field1;
        private readonly FieldInfo field2;
        private readonly FieldInfo field3;
        private readonly FieldInfo field4;

        public XoshiroStarSerializer()
        {
            var type = typeof(XoShiRo256starstar);

            // Using reflection to be able to force read the fields
            field1 = type.GetField("s0", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new Exception("Xoshiro private fields have changed");
            field2 = type.GetField("s1", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new Exception("Xoshiro private fields have changed");
            field3 = type.GetField("s2", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new Exception("Xoshiro private fields have changed");
            field4 = type.GetField("s3", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new Exception("Xoshiro private fields have changed");
        }

        public void Write(ISArchiveWriter writer, XoShiRo256starstar instance)
        {
            writer.WriteObjectHeader((ArchiveObjectType)ThriveArchiveObjectType.XoShiRo256StarStar, false, false, false,
                false, XOSHIRO_SERIALIZATION_VERSION);

            writer.Write((ulong)(field1.GetValue(instance) ?? throw new Exception("Xoshiro field read failed")));
            writer.Write((ulong)(field2.GetValue(instance) ?? throw new Exception("Xoshiro field read failed")));
            writer.Write((ulong)(field3.GetValue(instance) ?? throw new Exception("Xoshiro field read failed")));
            writer.Write((ulong)(field4.GetValue(instance) ?? throw new Exception("Xoshiro field read failed")));
        }

        public XoShiRo256starstar Read(ISArchiveReader reader, ushort version)
        {
            if (version is > XOSHIRO_SERIALIZATION_VERSION or <= 0)
                throw new InvalidArchiveVersionException(version, XOSHIRO_SERIALIZATION_VERSION);

            return new XoShiRo256starstar([
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
            ]);
        }
    }
}
