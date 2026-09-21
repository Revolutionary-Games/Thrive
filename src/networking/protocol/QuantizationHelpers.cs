using System;

/// <summary>
///   Conversions between floats and the smaller integer representations used on the wire to save on bandwidth
/// </summary>
public static class QuantizationHelpers
{
    public const float ANGLE_RANGE = MathF.PI * 2;

    public static ushort ToQuantized16(float value, float min, float max)
    {
        return (ushort)(NormalizeToRange(value, min, max) * ushort.MaxValue + 0.5f);
    }

    public static float FromQuantized16(ushort value, float min, float max)
    {
        return min + (max - min) * ((float)value / ushort.MaxValue);
    }

    public static byte ToQuantized8(float value, float min, float max)
    {
        return (byte)(NormalizeToRange(value, min, max) * byte.MaxValue + 0.5f);
    }

    public static float FromQuantized8(byte value, float min, float max)
    {
        return min + (max - min) * ((float)value / byte.MaxValue);
    }

    public static ushort AngleToQuantized16(float radians)
    {
        float wrapped = radians % ANGLE_RANGE;

        if (wrapped < 0)
            wrapped += ANGLE_RANGE;

        return ToQuantized16(wrapped, 0, ANGLE_RANGE);
    }

    public static float AngleFromQuantized16(ushort value)
    {
        return FromQuantized16(value, 0, ANGLE_RANGE);
    }

    private static float NormalizeToRange(float value, float min, float max)
    {
        if (max <= min)
            throw new ArgumentException("Quantization range is empty");

        float normalized = (value - min) / (max - min);

        if (normalized < 0)
            return 0;

        if (normalized > 1)
            return 1;

        return normalized;
    }
}
