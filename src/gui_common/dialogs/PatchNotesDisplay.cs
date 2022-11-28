using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

// TODO: see <insert issue link>
// [Tool]
public class PatchNotesDisplay : CustomDialog
{
    [Export]
    public NodePath TextsContainerPath = null!;

    //private (string Heading, Func<string> Content) patchNotes = (null!, null!);
    private (string Heading, string Content) patchNotes = (null!, null!);

    private bool patchNotesLoaded;

    private Container textsContainer = null!;

    /// <summary>
    /// Loads the specific patch notes
    /// </sumary>
    /// <returns>The patch notes text</returns>
    public static string LoadPatchNotesFile()
    {
        // TODO May need to add more
        return LoadFile(Constants.PATCH_NOTES_FILE);
    }

    public override void _Ready()
    {
        textsContainer = GetNode<Container>(TextsContainerPath);

        patchNotes = ("Temporary", LoadFile(Constants.PATCH_NOTES_FILE));
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
        using System.IO.StreamReader r = new System.IO.StreamReader(Constants.PATCH_NOTES_FILE);
        var reader = new File();
        //if (reader.Open(file, Godot.File.ModeFlags.Read) == Error.Ok)
        //{
        return r.ReadToEnd();

            // List<Item> items = JsonConvert.DeserializeObject<List<Item>>(json);
        //}
        GD.PrintErr("Can't load file to show in licenses: ", file);
        return "Missing file to show here!";
    }

    private void LoadPatchNotesText()
    {
        var heading = new Label { Text = patchNotes.Heading };
        heading.AddFontOverride("font", GetFont("lato_bold_regular", "Fonts"));
        textsContainer.AddChild(heading);

        var content = new Label
        {
            Text = patchNotes.Content,
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
