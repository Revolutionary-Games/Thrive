using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Godot;

public class PatchNotesList : VBoxContainer
{
    private bool showAll = true;

    private string? filterOldestVersion;
    private string? filterNewestVersion;

    private bool styleWithBackground = true;
    private bool titlesAreClickable = true;
    private bool addTrailingLinkToPatchNotesToViewIt = true;

    private bool dirty = true;

    private Task<List<(string Version, VersionPatchNotes Notes)>>? thingsToShowComputeResults;

    private StyleBoxFlat? itemBackground;

#pragma warning disable CA2213
    private PackedScene customRichTextScene = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   If true shows all of the existing patch notes
    /// </summary>
    [Export]
    public bool ShowAll
    {
        get => showAll;
        set
        {
            if (showAll == value)
                return;

            showAll = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   If set and <see cref="ShowAll"/> is not true then versions older than this are not displayed
    /// </summary>
    [Export]
    public string? FilterOldestVersion
    {
        get => filterOldestVersion;
        set
        {
            if (filterOldestVersion == value)
                return;

            filterOldestVersion = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   Same as <see cref="FilterOldestVersion"/> but instead sets the newest version to show
    /// </summary>
    [Export]
    public string? FilterNewestVersion
    {
        get => filterNewestVersion;
        set
        {
            if (filterNewestVersion == value)
                return;

            filterNewestVersion = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   When true the patch note items will have their own background (set to false when contained in something that
    ///   already has a background)
    /// </summary>
    [Export]
    public bool StyleWithBackground
    {
        get => styleWithBackground;
        set
        {
            if (styleWithBackground == value)
                return;

            styleWithBackground = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   When true the version numbers are clickable links to open the full info about the release in a browser
    /// </summary>
    [Export]
    public bool TitlesAreClickable
    {
        get => titlesAreClickable;
        set
        {
            if (titlesAreClickable == value)
                return;

            titlesAreClickable = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   When true each version will have a bit of trailing text and a link to the full release info
    /// </summary>
    [Export]
    public bool AddTrailingLinkToPatchNotesToViewIt
    {
        get => addTrailingLinkToPatchNotesToViewIt;
        set
        {
            if (addTrailingLinkToPatchNotesToViewIt == value)
                return;

            addTrailingLinkToPatchNotesToViewIt = value;
            dirty = true;
        }
    }

    // Note that these fonts are properties to get these be in logical order in the Godot editor, modifying these
    // doesn't trigger a rebuild of the display data

#pragma warning disable CA2213
    [Export]
    public Font TitleFont { get; set; } = null!;

    [Export]
    public Font SubHeadingFont { get; set; } = null!;

    [Export]
    public Font TrailingVisitLinkFont { get; set; } = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        customRichTextScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        itemBackground = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.2f, 0.7f),
            BorderWidthBottom = 0,
            BorderWidthLeft = 0,
            BorderWidthRight = 0,
            BorderWidthTop = 0,

            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 0,

            ContentMarginTop = 2,
            ContentMarginBottom = 4,
            ContentMarginLeft = 4,
            ContentMarginRight = 3,
        };
    }

    public override void _Process(float delta)
    {
        if (thingsToShowComputeResults != null)
        {
            // Process calculated information to show once it is ready
            if (!thingsToShowComputeResults.IsCompleted)
                return;

            BuildNotesList(thingsToShowComputeResults.Result);

            thingsToShowComputeResults.Dispose();
            thingsToShowComputeResults = null;
            return;
        }

        // Skip rebuilding if this has not changed or is visible
        if (!dirty || !IsVisibleInTree())
            return;

        // Start the process of refreshing the data here
        var oldest = FilterOldestVersion;
        var newest = FilterNewestVersion;

        if (ShowAll)
        {
            oldest = null;
            newest = null;
        }

        thingsToShowComputeResults =
            new Task<List<(string Version, VersionPatchNotes Notes)>>(() => CalculateVersionsToShow(oldest, newest));
        TaskExecutor.Instance.AddTask(thingsToShowComputeResults);

        this.QueueFreeChildren();

        dirty = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            itemBackground?.Dispose();

            thingsToShowComputeResults?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static List<(string Version, VersionPatchNotes Notes)> CalculateVersionsToShow(string? oldest,
        string? newest)
    {
        List<(string Version, VersionPatchNotes Notes)> result = new();

        foreach (var patchNote in SimulationParameters.Instance.GetPatchNotes())
        {
            // Skip if filtered out by being too new or old
            if (oldest != null && VersionUtils.Compare(patchNote.Key, oldest) < 0)
                continue;

            if (newest != null && VersionUtils.Compare(patchNote.Key, newest) > 0)
                continue;

            // TODO: we could maybe do the markdown to bbcode conversion already here to do it in a background thread
            result.Add((patchNote.Key, patchNote.Value));
        }

        // Patch notes make most sense when ordered from latest to oldest
        result.Reverse();

        return result;
    }

    private void BuildNotesList(List<(string Version, VersionPatchNotes Notes)> notes)
    {
        var stringBuilder = new StringBuilder();

        var changesHeading = TranslationServer.Translate("PATCH_NOTE_CHANGES_HEADING");
        var bulletPointTemplateText = TranslationServer.Translate("PATCH_NOTE_BULLET_POINT");
        var linkVisitTemplate = TranslationServer.Translate("PATCH_NOTE_LINK_VISIT_TEXT");

        var subHeadingFontPath = SubHeadingFont.ResourcePath;

        // This could use the same approach as ThriveFeedDisplayer to build only one object per frame, but
        // as this is usually empty or just shows the latest patch notes, that wouldn't help in the common case at all
        foreach (var (version, versionPatchNotes) in notes)
        {
            Container itemContainer;

            if (StyleWithBackground)
            {
                var panel = new PanelContainer();
                panel.AddStyleboxOverride("panel", itemBackground);

                itemContainer = panel;
            }
            else
            {
                itemContainer = new VBoxContainer();
            }

            var itemContentContainer = new VBoxContainer();
            itemContainer.AddChild(itemContentContainer);

            string titleText;

            if (TitlesAreClickable)
            {
                titleText = $"{Constants.CLICKABLE_TEXT_BBCODE}" +
                    $"[url={versionPatchNotes.ReleaseLink}]{version}[/url]{Constants.CLICKABLE_TEXT_BBCODE_END}";
            }
            else
            {
                titleText = version;
            }

            // This uses rich text purely to be clickable
            var title = customRichTextScene.Instance<CustomRichTextLabel>();
            title.BbcodeText = titleText;
            title.AddFontOverride("normal_font", TitleFont);

            itemContentContainer.AddChild(title);

            // Build the body text
            stringBuilder.Append(MarkdownToBbCodeConverter.Convert(versionPatchNotes.IntroductionText));

            stringBuilder.Append('\n');
            stringBuilder.Append('\n');

            stringBuilder.Append($"[font={subHeadingFontPath}]");
            stringBuilder.Append(changesHeading);
            stringBuilder.Append('\n');
            stringBuilder.Append("[/font]");

            foreach (var bulletPoint in versionPatchNotes.PatchNotes)
            {
                stringBuilder.Append(
                    bulletPointTemplateText.FormatSafe(MarkdownToBbCodeConverter.Convert(bulletPoint)));
                stringBuilder.Append('\n');
            }

            var bodyTextDisplayer = customRichTextScene.Instance<CustomRichTextLabel>();

            bodyTextDisplayer.BbcodeText = stringBuilder.ToString();
            stringBuilder.Clear();

            itemContentContainer.AddChild(bodyTextDisplayer);

            if (AddTrailingLinkToPatchNotesToViewIt)
            {
                var visitLink = customRichTextScene.Instance<CustomRichTextLabel>();

                visitLink.BbcodeText = linkVisitTemplate.FormatSafe(versionPatchNotes.ReleaseLink);

                visitLink.AddFontOverride("normal_font", TrailingVisitLinkFont);

                itemContentContainer.AddChild(visitLink);
            }

            AddChild(itemContainer);
        }
    }
}
