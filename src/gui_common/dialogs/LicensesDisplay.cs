﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
// [Tool]
/// <summary>
///   Shows the game licenses
/// </summary>
public partial class LicensesDisplay : CustomWindow
{
#pragma warning disable CA2213
    [Export]
    public LabelSettings HeadingFont = null!;

    [Export]
    public LabelSettings ContentFont = null!;
#pragma warning restore CA2213

    private List<(string Heading, Func<string> Content)> licensesToShow = null!;

    private bool licensesLoaded;

#pragma warning disable CA2213
    [Export]
    private Container textsContainer = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Loads the Steam version specific readme by combining the special Steam file and the normal libraries list
    /// </summary>
    /// <returns>The license text</returns>
    public static string LoadSteamLicenseFile()
    {
        var steam = LoadFile(Constants.STEAM_LICENSE_FILE);
        var normal = LoadFile(Constants.LICENSE_FILE);

        var regex = new Regex(".*(In addition to Godot Engine, Thrive uses the following.+)$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var match = regex.Match(normal);

        string extraText = "Error library list not found";

        if (match.Success)
        {
            extraText = match.Groups[1].Value;
        }
        else
        {
            GD.PrintErr("Failed to insert Steam version used library licenses list");
        }

        return steam + extraText;
    }

    public override void _Ready()
    {
        bool isSteamVersion = SteamHandler.IsTaggedSteamRelease();

        // These don't react to language change, but I doubt it's important enough to fix
        licensesToShow = new List<(string Heading, Func<string> Content)>
        {
            (string.Empty, isSteamVersion ? LoadSteamLicenseFile : () => LoadFile(Constants.LICENSE_FILE)),
            (string.Empty, () => LoadFile(Constants.ASSETS_README)),
            (string.Empty, () => LoadFile(Constants.ASSETS_LICENSE_FILE)),
            (string.Empty, () => LoadFile(Constants.OFL_LICENSE_FILE)),
            (string.Empty, () => LoadFile(Constants.EXTRA_THRIVE_LICENSE_FILE)),
            (string.Empty, () => LoadFile(Constants.GODOT_LICENSE_FILE)),
            (string.Empty, () => LoadFile(Constants.RUNTIME_LICENSE_FILE)),
        };

        if (!isSteamVersion)
        {
            licensesToShow.Add((Localization.Translate("GPL_LICENSE_HEADING"),
                () => LoadFile(Constants.GPL_LICENSE_FILE)));
        }
    }

    public override void _Process(double delta)
    {
        // Keep the license texts only loaded when this is visible
        if (IsVisibleInTree())
        {
            if (licensesLoaded)
                return;

            LoadLicenseTexts();
            licensesLoaded = true;
        }
        else
        {
            if (!licensesLoaded)
                return;

            textsContainer.QueueFreeChildren();

            licensesLoaded = false;
        }
    }

    private static string LoadFile(string file)
    {
        using var reader = FileAccess.Open(file, FileAccess.ModeFlags.Read);

        if (reader != null)
        {
            return reader.GetAsText();
        }

        GD.PrintErr("Can't load file to show in licenses: ", file);
        return "Missing file to show here!";
    }

    private void LoadLicenseTexts()
    {
        foreach (var licenseTuple in licensesToShow)
        {
            var heading = new Label { Text = licenseTuple.Heading };
            heading.LabelSettings = HeadingFont;
            textsContainer.AddChild(heading);

            var content = new Label
            {
                Text = licenseTuple.Content(),
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,

                // Increase the initial size to make loading faster
                Size = new Vector2(5000, 0),
            };

            content.LabelSettings = ContentFont;
            textsContainer.AddChild(content);

            textsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 5) });
        }
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
