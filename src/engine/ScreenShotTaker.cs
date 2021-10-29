using System;
using System.Globalization;
using Godot;

/// <summary>
///   Singleton handling screenshot taking
/// </summary>
public class ScreenShotTaker : NodeWithInput
{
    private static ScreenShotTaker instance;

    private Image screenshotImage;
    private Steps step;

    private ScreenShotTaker()
    {
        instance = this;
    }

    private enum Steps
    {
        Start,
        Wait,
        TakeScreenshot,
        Save,
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
        TakeScreenshot();
    }

    /// <summary>
    ///   Takes and saves a screenshot
    /// </summary>
    /// <returns>The path of the screenshot on success, null on failure</returns>
    public string TakeAndSaveScreenShot()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        var img = GetViewportTextureImage();

        return SaveScreenshot(img);
    }

    /// <summary>
    ///   Takes an image of the current viewport
    /// </summary>
    /// <returns>The image</returns>
    public Image GetViewportTextureImage()
    {
        var image = GetViewport().GetTexture().GetData();

        // TODO: do we always need this?
        image.FlipY();

        return image;
    }

    private string SaveScreenshot(Image img)
    {
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

    private void TakeScreenshot()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        if (ColourblindScreenFilter.Instance.Visible)
        {
            step = Steps.Start;
            Step();
            return;
        }

        SaveScreenshot(GetViewportTextureImage());
    }

    private void Step()
    {
        switch (step)
        {
            case Steps.Start:
                ColourblindScreenFilter.Instance.Hide();
                step = Steps.Wait;
                break;
            case Steps.Wait:
                step = Steps.TakeScreenshot;
                break;
            case Steps.TakeScreenshot:
                screenshotImage = GetViewportTextureImage();
                ColourblindScreenFilter.Instance.Show();
                step = Steps.Save;
                break;
            case Steps.Save:
                SaveScreenshot(screenshotImage);
                screenshotImage.Dispose();
                screenshotImage = null;
                return;
        }

        Invoke.Instance.Queue(Step);
    }
}
