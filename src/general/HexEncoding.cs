using System;
using System.Runtime.CompilerServices;

/// <summary>
///   Simple implementation of hex encoding and decoding
/// </summary>
public static class HexEncoding
{
    /// <summary>
    ///   Encodes a set of bytes as hex string in UTF8 format
    /// </summary>
    /// <param name="data">Where to read and put the result in</param>
    /// <param name="length">How much of the data is to be used</param>
    /// <param name="bytesWritten">How many bytes are written, on success should be equal to twice the length</param>
    /// <exception cref="ArgumentException">If not enough space</exception>
    public static void EncodeToHexInPlace(byte[] data, int length, out int bytesWritten)
    {
        if (length < 1)
        {
            bytesWritten = 0;
            return;
        }

        if (data.Length < length * 2)
            throw new ArgumentException("Hex encoding takes double the amount of space");

        int outputWrite = length * 2 - 1;

        for (int i = length - 1; i >= 0; --i)
        {
#if DEBUG
            if (outputWrite <= i)
                throw new Exception("Output write caught read point, this shouldn't happen");
#endif

            byte currentData = data[i];

            // As we write in reverse order, we put the high nibble first
            data[outputWrite--] = ByteToHexUtf8((byte)(currentData >> 4));
            data[outputWrite--] = ByteToHexUtf8(currentData);
        }

        bytesWritten = length * 2;
    }

    /// <summary>
    ///   Decodes hex encoded UTF8 text in place to raw bytes
    /// </summary>
    /// <param name="data">Input and the result</param>
    /// <param name="length">How many bytes the input is</param>
    /// <param name="outBytes">How many bytes have been written</param>
    /// <returns>False if invalid characters are encountered (or input is empty)</returns>
    public static bool DecodeFromHexInPlace(byte[] data, int length, out int outBytes)
    {
        outBytes = 0;

        if (length < 1)
            return false;

        for (int i = 0; i < length; i += 2)
        {
            // Low nibble
            if (!HexToNibble(data[i], out var fullByte))
                return false;

            if (i + 1 >= length)
            {
                // Uneven number of hex characters, we'll allow this but the high nibble stays all zeros
                data[outBytes++] = fullByte;
            }
            else
            {
                // High nibble
                if (!HexToNibble(data[i + 1], out var nibble))
                    return false;

                // Combine and write to result
                data[outBytes++] = (byte)(fullByte | nibble << 4);
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ByteToHexUtf8(byte value)
    {
        return (value & 0x0F) switch
        {
            0 => (byte)'0',
            1 => (byte)'1',
            2 => (byte)'2',
            3 => (byte)'3',
            4 => (byte)'4',
            5 => (byte)'5',
            6 => (byte)'6',
            7 => (byte)'7',
            8 => (byte)'8',
            9 => (byte)'9',
            10 => (byte)'a',
            11 => (byte)'b',
            12 => (byte)'c',
            13 => (byte)'d',
            14 => (byte)'e',
            15 => (byte)'f',

            // This should never hit thanks to the bit mask above... but to silence the compiler this is here
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HexToNibble(byte value, out byte nibble)
    {
        switch ((char)value)
        {
            case '0':
                nibble = 0;
                break;
            case '1':
                nibble = 1;
                break;
            case '2':
                nibble = 2;
                break;
            case '3':
                nibble = 3;
                break;
            case '4':
                nibble = 4;
                break;
            case '5':
                nibble = 5;
                break;
            case '6':
                nibble = 6;
                break;
            case '7':
                nibble = 7;
                break;
            case '8':
                nibble = 8;
                break;
            case '9':
                nibble = 9;
                break;
            case 'a':
            case 'A':
                nibble = 10;
                break;
            case 'b':
            case 'B':
                nibble = 11;
                break;
            case 'c':
            case 'C':
                nibble = 12;
                break;
            case 'd':
            case 'D':
                nibble = 13;
                break;
            case 'e':
            case 'E':
                nibble = 14;
                break;
            case 'f':
            case 'F':
                nibble = 15;
                break;
            default:
                // Unknown character that is not valid hex
                nibble = byte.MaxValue;
                return false;
        }

        return true;
    }
}
