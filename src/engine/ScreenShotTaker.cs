using System;
using System.Globalization;
using Godot;

/// <summary>
///   Singleton handling screenshot taking
/// </summary>
public class ScreenShotTaker : NodeWithInput
{
    private static ScreenShotTaker instance;
    private bool isCurrentlyTakingScreenshot;
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

    private void SaveScreenshot(Image img)
    {
        var filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ffff", CultureInfo.CurrentCulture) + ".png";

        var path = PathUtils.Join(Constants.SCREENSHOT_FOLDER, filename);

        var error = img.SavePng(path);

        if (error != Error.Ok)
        {
            GD.PrintErr("Saving screenshot failed ", error);
            return;
        }

        GD.Print("Saved screenshot: ", path);
    }

    /// <summary>
    ///   Takes and saves a screenshot
    /// </summary>
    private void TakeScreenshot()
    {
        if (isCurrentlyTakingScreenshot)
        {
            GD.Print("Already in the process of taking a screenshot.");
            return;
        }

        isCurrentlyTakingScreenshot = true;
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        // If ScreenFilter is active, turn it of before taking a screenshot.
        if (ColourblindScreenFilter.Instance.Visible)
        {
            step = Steps.Start;
            ScreenFilterScreenshotStepper();
            return;
        }

        SaveScreenshot(GetViewportTextureImage());
        isCurrentlyTakingScreenshot = false;
    }

    /// <summary>
    ///   Invokes itself to:
    ///   1: Hide the ScreenFilter and wait a frame.
    ///   2: Wait another frame.
    ///   3: Take the screenshot, show the filter and wait a frame so the filter can show itself again faster.
    ///   4: Save the screenshot.
    /// </summary>
    private void ScreenFilterScreenshotStepper()
    {
        switch (step)
        {
            case Steps.Start:
                GD.Print("Start");
                ColourblindScreenFilter.Instance.Hide();
                step = Steps.Wait;
                break;
            case Steps.Wait:
                GD.Print("Wait");
                step = Steps.TakeScreenshot;
                break;
            case Steps.TakeScreenshot:
                GD.Print("TakeSceenshot");
                screenshotImage = GetViewportTextureImage();
                ColourblindScreenFilter.Instance.Show();
                step = Steps.Save;
                break;
            case Steps.Save:
                GD.Print("Save");
                SaveScreenshot(screenshotImage);
                screenshotImage.Dispose();
                screenshotImage = null;
                isCurrentlyTakingScreenshot = false;
                return;
        }

        Invoke.Instance.Queue(ScreenFilterScreenshotStepper);
    }
}
