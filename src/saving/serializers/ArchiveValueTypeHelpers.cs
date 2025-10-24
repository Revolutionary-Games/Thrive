﻿using System;
using Godot;
using SharedBase.Archive;

public static class ArchiveValueTypeHelpers
{
    // If any of the base writers change, then also all *dependent* objects need their versions updated
    public const int SERIALIZATION_VERSION = 1;

    public static void Write(this ISArchiveWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public static Vector2 ReadVector2(this ISArchiveReader reader)
    {
        return new Vector2(reader.ReadFloat(), reader.ReadFloat());
    }

    public static void Write(this ISArchiveWriter writer, Vector2I value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public static Vector2I ReadVector2I(this ISArchiveReader reader)
    {
        return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
    }

    public static object ReadVector2IBoxed(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return ReadVector2I(reader);
    }

    public static void WriteVector2IBoxed(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.Vector2I)
            throw new NotSupportedException();

        writer.WriteObjectHeader(type, false, false, false, false, SERIALIZATION_VERSION);
        writer.Write((Vector2I)obj);
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

    public static void Write(this ISArchiveWriter writer, Hex value)
    {
        writer.Write(value.Q);
        writer.Write(value.R);
    }

    public static Hex ReadHex(this ISArchiveReader reader)
    {
        return new Hex(reader.ReadInt32(), reader.ReadInt32());
    }
}
