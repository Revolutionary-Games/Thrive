using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
///   Helper functions for marshalling types and byte encodings.
/// </summary>
public static class MarshalUtils
{
    /// <summary>
    ///   Encodes a boolean into a byte with 1 representing true and 0 representing false.
    /// </summary>
    public static byte ToByte(this bool boolean)
    {
        return (byte)(boolean ? 1 : 0);
    }

    /// <summary>
    ///   Encodes an array of 8 booleans into a byte with each bits either representing true or false.
    /// </summary>
    public static byte ToByte(this bool[] booleans)
    {
        if (booleans.Length > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(booleans),
                "Array length must be less or equal to 8 (1 byte)");
        }

        byte result = 0;

        for (int i = 0; i < booleans.Length; ++i)
        {
            result |= (byte)(booleans[i].ToByte() << i);
        }

        return result;
    }

    /// <summary>
    ///   Encodes an array of variable length booleans into an array of bytes with each bits either representing
    ///   true (1) or false (0).
    /// </summary>
    public static byte[] ToByteArray(this bool[] booleans)
    {
        var bits = new BitArray(booleans);
        var result = new byte[Math.Max(1, bits.Length / 8)];
        bits.CopyTo(result, 0);
        return result;
    }

    /// <summary>
    ///   Gets the value of a boolean from the encoded byte.
    /// </summary>
    public static bool ToBoolean(this byte value)
    {
        if (value <= 0)
            return false;

        return true;
    }

    /// <summary>
    ///   Gets the value of a boolean from the specified bit offset in the encoded byte.
    /// </summary>
    public static bool ToBoolean(this byte value, int bitOffset)
    {
        return (value & (1 << bitOffset)) != 0;
    }

    public static bool[] ToBooleanArray(this byte[] value)
    {
        var result = new List<bool>(value.Length * 8);

        for (int i = 0; i < value.Length; ++i)
        {
            var batch = new bool[8];

            for (int j = 8; j > 0; --j)
            {
                batch[j] = value[i].ToBoolean(i);
            }

            result.AddRange(batch);
        }

        return result.ToArray();
    }

    public static void EncodeUInt16(this byte[] buffer, ushort value)
    {
        for (int i = 0; i < 2; ++i)
        {
            buffer[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }

    public static void EncodeUInt32(this byte[] buffer, uint value)
    {
        for (int i = 0; i < 4; ++i)
        {
            buffer[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }

    public static void EncodeUInt64(this byte[] buffer, ulong value)
    {
        for (int i = 0; i < 8; ++i)
        {
            buffer[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }

    public static void EncodeSingle(this byte[] buffer, float value)
    {
        var marshal = new MarshalFloat
        {
            Float = value,
        };

        buffer.EncodeUInt32(marshal.Int);
    }

    public static ushort DecodeUInt16(this byte[] buffer)
    {
        ushort result = 0;

        for (int i = 0; i < 2; ++i)
        {
            ushort bit = buffer[i];
            bit <<= i * 8;
            result |= bit;
        }

        return result;
    }

    public static uint DecodeUInt32(this byte[] buffer)
    {
        uint result = 0;

        for (int i = 0; i < 4; ++i)
        {
            uint bit = buffer[i];
            bit <<= i * 8;
            result |= bit;
        }

        return result;
    }

    public static ulong DecodeUInt64(this byte[] buffer)
    {
        ulong result = 0;

        for (int i = 0; i < 8; ++i)
        {
            ulong bit = (ulong)(buffer[i] & 0xFF);
            bit <<= i * 8;
            result |= bit;
        }

        return result;
    }

    public static float DecodeSingle(this byte[] buffer)
    {
        var marshal = new MarshalFloat
        {
            Int = buffer.DecodeUInt32(),
        };

        return marshal.Float;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MarshalFloat
    {
        [FieldOffset(0)]
        public uint Int;

        [FieldOffset(0)]
        public float Float;
    }
}
