using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Path = System.IO.Path;

/// <summary>
///   Singleton handling screenshot taking
/// </summary>
public partial class ScreenShotTaker : NodeWithInput
{
    private static ScreenShotTaker? instance;
    private bool isCurrentlyTakingScreenshot;
    private Step step;

    private ScreenShotTaker()
    {
        instance = this;
    }

    private enum Step
    {
        Start,
        Wait,
        TakeAndSaveScreenshot,
    }

    public static ScreenShotTaker Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        // Keep this node running while paused
        ProcessMode = ProcessModeEnum.Always;
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
    public Image GetViewportTextureAsImage()
    {
        var image = GetViewport().GetTexture().GetImage();

        // Viewport is no longer flipped in Godot 4 (hopefully this is not renderer specific)
        // image.FlipY();

        return image;
    }

    private void SaveScreenshotInBackground(Image image)
    {
        TaskExecutor.Instance.AddTask(new Task(() => SaveScreenshot(image)));
    }

    private void SaveScreenshot(Image image)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        var filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ffff", CultureInfo.CurrentCulture) + ".png";

        var path = Path.Combine(Constants.SCREENSHOT_FOLDER, filename);

        var error = image.SavePng(path);

        if (error != Error.Ok)
        {
            GD.PrintErr("Saving screenshot failed ", error);
            return;
        }

        GD.Print("Saved screenshot: ", path);

        image.Dispose();
        isCurrentlyTakingScreenshot = false;
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

        if (SteamHandler.Instance.IsLoaded)
        {
            if (Input.IsKeyPressed(Key.F12))
            {
                GD.Print("Ignoring F12 as Steam is probably taking a screenshot with that");
                return;
            }
        }

        isCurrentlyTakingScreenshot = true;

        // If ScreenFilter is active, turn it off before taking a screenshot.
        if (ColourblindScreenFilter.Instance.Visible)
        {
            step = Step.Start;
            ScreenFilterScreenshotStepper();
            return;
        }

        SaveScreenshotInBackground(GetViewportTextureAsImage());
    }

    /// <summary>
    ///   Invokes itself to:
    ///   1: Hide the ScreenFilter and wait a frame.
    ///   2: Wait another frame.
    ///   3: Take the screenshot, show the filter and then save the screenshot in a task to not block the game.
    /// </summary>
    private void ScreenFilterScreenshotStepper()
    {
        switch (step)
        {
            case Step.Start:
                ColourblindScreenFilter.Instance.Hide();
                step = Step.Wait;
                break;
            case Step.Wait:
                step = Step.TakeAndSaveScreenshot;
                break;
            case Step.TakeAndSaveScreenshot:
                SaveScreenshotInBackground(GetViewportTextureAsImage());
                ColourblindScreenFilter.Instance.Show();
                return;
            default:
                throw new InvalidOperationException("invalid step for ScreenShotTaker");
        }

        Invoke.Instance.Queue(ScreenFilterScreenshotStepper);
    }
}
