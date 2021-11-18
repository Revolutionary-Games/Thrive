using System.Collections.Generic;
using Godot;

public class LicensesDisplay : PanelContainer
{
    private List<(string Heading, string File)> licensesToShow;

    private bool licensesLoaded;

    private Container textsContainer;

    [Export]
    public NodePath TextsContainerPath { get; set; }

    public override void _Ready()
    {
        textsContainer = GetNode<Container>(TextsContainerPath);

        bool isSteamVersion = SteamHandler.IsTaggedSteamRelease();

        // These don't react to language change, but I doubt it's important enough to fix
        licensesToShow = new List<(string Heading, string File)>
        {
            (string.Empty, isSteamVersion ? Constants.STEAM_LICENSE_FILE : Constants.LICENSE_FILE),
            (string.Empty, Constants.ASSETS_README),
            (string.Empty, Constants.ASSETS_LICENSE_FILE),
            (string.Empty, Constants.OFL_LICENSE_FILE),
            (string.Empty, Constants.GODOT_LICENSE_FILE),
        };

        if (!isSteamVersion)
            licensesToShow.Add((TranslationServer.Translate("GPL_LICENSE_HEADING"), Constants.GPL_LICENSE_FILE));
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
            var heading = new Label { Text = licenseTuple.Heading };
            heading.AddFontOverride("font", GetFont("lato_bold_regular", "Fonts"));
            textsContainer.AddChild(heading);

            string text;
            using var reader = new File();

            if (reader.Open(licenseTuple.File, File.ModeFlags.Read) == Error.Ok)
            {
                text = reader.GetAsText();
            }
            else
            {
                text = "Missing file to show here!";
                GD.PrintErr("Can't load file to show in licenses: ", licenseTuple.File);
            }

            var content = new Label
            {
                Text = text,
                Align = Label.AlignEnum.Left,
                Autowrap = true,
            };

            content.AddFontOverride("font", GetFont("lato_normal", "Fonts"));
            textsContainer.AddChild(content);

            textsContainer.AddChild(new Control { RectMinSize = new Vector2(0, 5) });
        }
    }
}
