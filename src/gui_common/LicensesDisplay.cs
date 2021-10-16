using System.Collections.Generic;
using Godot;

public class LicensesDisplay : ScrollContainer
{
    private List<(string heading, string file)> licensesToShow;

    private bool licensesLoaded;

    private Container textsContainer;

    [Export]
    public NodePath TextsContainerPath { get; set; }

    public override void _Ready()
    {
        textsContainer = GetNode<Container>(TextsContainerPath);

        // These don't react to language change, but I doubt it's important enough to fix
        licensesToShow = new List<(string heading, string file)>
        {
            (string.Empty, Constants.LICENSE_FILE),
            (string.Empty, Constants.ASSETS_README),
            (string.Empty, Constants.ASSETS_LICENSE_FILE),
            (string.Empty, Constants.OFL_LICENSE_FILE),
            (string.Empty, Constants.GODOT_LICENSE_FILE),
            (TranslationServer.Translate("GPL_LICENSE_HEADING"), Constants.GPL_LICENSE_FILE),
        };
    }

    public override void _Process(float delta)
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

    private void LoadLicenseTexts()
    {
        foreach (var licenseTuple in licensesToShow)
        {
            textsContainer.AddChild(new Label { Text = licenseTuple.heading });

            string text;
            using var reader = new File();

            if (reader.Open(licenseTuple.file, File.ModeFlags.Read) == Error.Ok)
            {
                text = reader.GetAsText();
            }
            else
            {
                text = "Missing file to show here!";
                GD.PrintErr("Can't load file to show in licenses: ", licenseTuple.file);
            }

            textsContainer.AddChild(new Label
            {
                Text = text,
                Align = Label.AlignEnum.Left,
                Autowrap = true,
            });

            textsContainer.AddChild(new Control { RectMinSize = new Vector2(0, 5) });
        }
    }
}
