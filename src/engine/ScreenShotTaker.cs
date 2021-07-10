using System;
using System.Globalization;
using Godot;

/// <summary>
///   Singleton handling screenshot taking
/// </summary>
public class ScreenShotTaker : NodeWithInput
{
    private static ScreenShotTaker instance;

    private ScreenShotTaker()
    {
        instance = this;
    }

    public static ScreenShotTaker Instance => instance;

    public override void _Ready()
    {
        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    [RunOnKeyDown("screenshot", OnlyUnhandled = false)]
    public void TakeScreenshotPressed()
    {
        GD.Print("Taking a screenshot");
        TakeAndSaveScreenShot();
    }

    /// <summary>
    ///   Takes and saves a screenshot
    /// </summary>
    /// <returns>The path of the screenshot on success, null on failure</returns>
    public string TakeAndSaveScreenShot()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        var img = TakeScreenshot();
        var filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ffff", CultureInfo.CurrentCulture) + ".png";

        var path = PathUtils.Join(Constants.SCREENSHOT_FOLDER, filename);

        var error = img.SavePng(path);

        if (error != Error.Ok)
        {
            GD.PrintErr("Saving screenshot failed ", error);
            return null;
        }

        GD.Print("Saved screenshot: ", path);
        return path;
    }

    /// <summary>
    ///   Takes an image of the current viewport
    /// </summary>
    /// <returns>The image</returns>
    public Image TakeScreenshot()
    {
        var image = GetViewport().GetTexture().GetData();

        // TODO: do we always need this?
        image.FlipY();

        return image;
    }
}
