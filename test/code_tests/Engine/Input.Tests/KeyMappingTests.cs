namespace ThriveTest.Engine.Input.Tests;

using Godot;
using Xunit;

public class KeyMappingTests
{
    private const string XboxControllerDiagramPath =
        "res://assets/textures/gui/xelu_prompts/Xbox Series X/XboxSeriesX_Diagram_Simple.png";
    private const string PlayStationControllerDiagramPath =
        "res://assets/textures/gui/xelu_prompts/PS5/PS5_Diagram_Simple.png";

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

    [Theory]
    [InlineData(ControllerType.Xbox360, XboxControllerDiagramPath)]
    [InlineData(ControllerType.XboxOne, XboxControllerDiagramPath)]
    [InlineData(ControllerType.XboxSeriesX, XboxControllerDiagramPath)]
    [InlineData(ControllerType.PlayStation3, PlayStationControllerDiagramPath)]
    [InlineData(ControllerType.PlayStation4, PlayStationControllerDiagramPath)]
    [InlineData(ControllerType.PlayStation5, PlayStationControllerDiagramPath)]
    public static void KeyMapping_ControllerDiagramMatchesControllerFamily(ControllerType controllerType,
        string expectedPath)
    {
        var previousControllerType = KeyPromptHelper.ActiveControllerType;

        try
        {
            KeyPromptHelper.ActiveControllerType = controllerType;
            Assert.Equal(expectedPath, KeyPromptHelper.GetPathForControllerDiagram());
        }
        finally
        {
            KeyPromptHelper.ActiveControllerType = previousControllerType;
        }
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
