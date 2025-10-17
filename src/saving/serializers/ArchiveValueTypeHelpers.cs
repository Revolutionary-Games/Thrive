using Godot;
using SharedBase.Archive;

public static class ArchiveValueTypeHelpers
{
    public static void Write(this ISArchiveWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public static Vector2 ReadVector2(this ISArchiveReader reader)
    {
        return new Vector2(reader.ReadFloat(), reader.ReadFloat());
    }

    public static void Write(this ISArchiveWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    public static Vector3 ReadVector3(this ISArchiveReader reader)
    {
        return new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
    }

    public static void Write(this ISArchiveWriter writer, Quaternion value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    public static Quaternion ReadQuaternion(this ISArchiveReader reader)
    {
        return new Quaternion(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
    }

    public static void Write(this ISArchiveWriter writer, Color value)
    {
        writer.Write(value.R);
        writer.Write(value.G);
        writer.Write(value.B);
        writer.Write(value.A);
    }

    public static Color ReadColor(this ISArchiveReader reader)
    {
        return new Color(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
    }
}
