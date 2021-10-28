using System;
using System.Globalization;
using System.Threading.Tasks;
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
    public async Task TakeScreenshotPressed()
    {
        GD.Print("Taking a screenshot");
        await TakeScreenshotAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///   Takes and saves a screenshot
    /// </summary>
    /// <returns>The path of the screenshot on success, null on failure</returns>
    public string TakeAndSaveScreenShot()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        var img = TakeScreenshot();

        return SaveScreenshot(img);
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

    private async Task TakeScreenshotAsync()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        bool wasColourblindScreenFilterVisible = ColourblindScreenFilter.Instance.Visible;
        if (wasColourblindScreenFilterVisible)
        {
            ColourblindScreenFilter.Instance.Hide();

            // two frames needed
            await ToSignal(GetTree(), "idle_frame");
            await ToSignal(GetTree(), "idle_frame");
        }

        using Image image = TakeScreenshot();

        if (wasColourblindScreenFilterVisible)
        {
            ColourblindScreenFilter.Instance.Show();
            await ToSignal(GetTree(), "idle_frame");
        }

        SaveScreenshot(image);
    }
}
