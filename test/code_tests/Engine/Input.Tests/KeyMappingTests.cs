namespace ThriveTest.Engine.Input.Tests;

using Godot;
using Xunit;

public class KeyMappingTests
{
    [Fact]
    public static void KeyMapping_CodeEnumConversionWorks()
    {
        Assert.Equal(Key.Escape, (Key)(ulong)Key.Escape);
        Assert.Equal(Key.A, (Key)(ulong)Key.A);
    }

    [Fact]
    public static void KeyMapping_CodeAndDevicePackingWorks()
    {
        Assert.Equal(((JoyButton)0, -1),
            SpecifiedInputKey.UnpackCodeAndDevice(SpecifiedInputKey.PackCodeWithDevice(0, -1)));

        Assert.Equal(((JoyButton)5, -1),
            SpecifiedInputKey.UnpackCodeAndDevice(SpecifiedInputKey.PackCodeWithDevice(5, -1)));

        Assert.Equal(((JoyButton)(-5), -1),
            SpecifiedInputKey.UnpackCodeAndDevice(SpecifiedInputKey.PackCodeWithDevice(-5, -1)));

        Assert.Equal(((JoyButton)5, 5),
            SpecifiedInputKey.UnpackCodeAndDevice(SpecifiedInputKey.PackCodeWithDevice(5, 5)));

        Assert.Equal(((JoyButton)155, 128),
            SpecifiedInputKey.UnpackCodeAndDevice(SpecifiedInputKey.PackCodeWithDevice(155, 128)));
    }

    [Fact]
    public static void KeyMapping_AxisPackingWorks()
    {
        Assert.Equal(((JoyAxis)1, -1, -1),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(1, -1, -1)));

        Assert.Equal(((JoyAxis)(-1), -1, -1),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(-1, -1, -1)));

        Assert.Equal(((JoyAxis)1, 1, -1),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(1, 1, -1)));

        Assert.Equal(((JoyAxis)5, 1, -1),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(5, 1, -1)));

        Assert.Equal(((JoyAxis)5, 1, 15),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(5, 1, 15)));

        Assert.Equal(((JoyAxis)150, 1, 128),
            SpecifiedInputKey.UnpackAxis(SpecifiedInputKey.PackAxisWithDirection(150, 1, 128)));
    }
}
