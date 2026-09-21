namespace ThriveTest.Networking.Tests;

using System;
using Godot;
using Xunit;

public class NetworkWriterTests
{
    [Fact]
    public static void NetworkWriter_RoundTripsBasicTypes()
    {
        var writer = new NetworkWriter();
        writer.Write(MessageType.Snapshot);
        writer.Write(true);
        writer.Write((byte)200);
        writer.Write((ushort)60000);
        writer.Write(-12345);
        writer.Write(4000000000U);
        writer.Write(1.5f);
        writer.Write(new Vector3(1, -2, 3.5f));
        writer.Write("a test name");

        var reader = ReaderFor(writer);

        Assert.Equal(MessageType.Snapshot, reader.ReadMessageType());
        Assert.True(reader.ReadBool());
        Assert.Equal(200, reader.ReadByte());
        Assert.Equal(60000, reader.ReadUInt16());
        Assert.Equal(-12345, reader.ReadInt32());
        Assert.Equal(4000000000U, reader.ReadUInt32());
        Assert.Equal(1.5f, reader.ReadFloat());
        Assert.Equal(new Vector3(1, -2, 3.5f), reader.ReadVector3());
        Assert.Equal("a test name", reader.ReadString());
        Assert.True(reader.AtEnd);
    }

    [Fact]
    public static void NetworkWriter_QuantizedValuesStayWithinTolerance()
    {
        var writer = new NetworkWriter();
        writer.WriteQuantized16(123.25f, -500, 500);
        writer.WriteQuantized8(0.75f, 0, 1);
        writer.WriteAngle(2.5f);

        var reader = ReaderFor(writer);

        // A 16 bit value over a 1000 unit range is accurate to well under a hundredth of a unit
        Assert.Equal(123.25f, reader.ReadQuantized16(-500, 500), 2);
        Assert.Equal(0.75f, reader.ReadQuantized8(0, 1), 2);
        Assert.Equal(2.5f, reader.ReadAngle(), 3);
    }

    /// <summary>
    ///   A quantized value outside its range must clamp rather than wrap, as wrapping would teleport an entity
    ///   to the other side of the arena
    /// </summary>
    [Fact]
    public static void NetworkWriter_QuantizedValueClampsOutsideRange()
    {
        var writer = new NetworkWriter();
        writer.WriteQuantized16(900, -100, 100);
        writer.WriteQuantized16(-900, -100, 100);

        var reader = ReaderFor(writer);

        Assert.Equal(100, reader.ReadQuantized16(-100, 100), 2);
        Assert.Equal(-100, reader.ReadQuantized16(-100, 100), 2);
    }

    [Fact]
    public static void NetworkWriter_NegativeAngleWrapsToSameDirection()
    {
        var writer = new NetworkWriter();
        writer.WriteAngle(-MathF.PI / 2);

        var reader = ReaderFor(writer);

        Assert.Equal(MathF.PI * 1.5f, reader.ReadAngle(), 3);
    }

    [Fact]
    public static void NetworkWriter_GrowsPastInitialCapacity()
    {
        var writer = new NetworkWriter(4);

        for (int i = 0; i < 100; ++i)
        {
            writer.Write(i);
        }

        var reader = ReaderFor(writer);

        for (int i = 0; i < 100; ++i)
        {
            Assert.Equal(i, reader.ReadInt32());
        }
    }

    [Fact]
    public static void NetworkWriter_OverwriteFixesUpCount()
    {
        var writer = new NetworkWriter();
        int countPosition = writer.Length;
        writer.Write((ushort)0);
        writer.Write(7);
        writer.OverwriteUInt16(countPosition, 1);

        var reader = ReaderFor(writer);

        Assert.Equal(1, reader.ReadUInt16());
        Assert.Equal(7, reader.ReadInt32());
    }

    /// <summary>
    ///   Reading past the end must throw rather than return junk, as a malicious client can send a short message
    /// </summary>
    [Fact]
    public static void NetworkReader_ThrowsOnTruncatedMessage()
    {
        var writer = new NetworkWriter();
        writer.Write((byte)1);

        var reader = ReaderFor(writer);
        reader.ReadByte();

        Assert.Throws<EndOfNetworkMessageException>(() => reader.ReadInt32());
    }

    private static NetworkReader ReaderFor(NetworkWriter writer)
    {
        var data = writer.WrittenData.ToArray();

        var reader = new NetworkReader();
        reader.SetData(data, data.Length);
        return reader;
    }
}
