using System;
using System.Collections.Generic;
using Godot;

// [Tool]
public class PatchNotesDisplay : CustomDialog
{
    [Export]
    public NodePath TextsContainerPath = null!;

    private (string Heading, Func<string> Content) patchNotes = (null!, null!);

    private bool patchNotesLoaded;

    private Container textsContainer = null!;

    private static Dictionary<string, string> patchNotesJson = null!;

    /// <summary>
    /// Loads the specific patch notes for the given or otherwise current version
    /// </sumary>
    /// <returns>The patch notes text</returns>
    public static string LoadPartialPatchNotesFile(string? version=null)
    {
        if (version == null)
        {
            version = Constants.Version;
        }

        string ret;

        if (!patchNotesJson.TryGetValue(version, out ret))
        {
            ret = "Failed to fetch patch notes";
        }

        return ret;
    }

    public static string LoadCompletePatchNotesFile()
    {
        string ret = "";

        foreach(KeyValuePair<string, string> entry in patchNotesJson)
        {
            ret += $"VERSION {entry.Key}:\n{entry.Value}\n\n";
        }

        return ret;
    }

    public override void _Ready()
    {
        textsContainer = GetNode<Container>(TextsContainerPath);

        InitDictionary();

        patchNotes = (string.Empty, () => LoadCompletePatchNotesFile());
    }

    public override void _Process(float delta)
    {
        if (IsVisibleInTree())
        {
            if (patchNotesLoaded)
                return;

            LoadPatchNotesText();
            patchNotesLoaded = true;
        }
        else
        {
            if (!patchNotesLoaded)
                return;

            textsContainer.QueueFreeChildren();
            patchNotesLoaded = false;
        }
    }

    private static string LoadFile(string file)
    {
        using var reader = new File();

        if (reader.Open(file, File.ModeFlags.Read) == Error.Ok)
        {
            return reader.GetAsText();
        }

        GD.PrintErr("Can't load file to show in patch notes: ", file);
        return "Missing file to show here!";
    }

    private void InitDictionary()
    {
        string text = LoadFile(Constants.PATCH_NOTES_FILE);

        patchNotesJson = ThriveJsonConverter.Instance.DeserializeObject<Dictionary<String, String>>(text) ??
            throw new Newtonsoft.Json.JsonException("Current version is missing from patch notes");
    }

    private void LoadPatchNotesText()
    {
        var heading = new Label { Text = patchNotes.Heading };
        heading.AddFontOverride("font", GetFont("lato_bold_regular", "Fonts"));
        textsContainer.AddChild(heading);

        var content = new Label
        {
            Text = patchNotes.Content(),
            Align = Label.AlignEnum.Left,
            Autowrap = true,
        };

        content.AddFontOverride("font", GetFont("lato_normal", "Fonts"));
        textsContainer.AddChild(content);

        textsContainer.AddChild(new Control { RectMinSize = new Vector2(0, 5) });
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
