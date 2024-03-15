using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Displays a Thrive news feed that is fetched from the internet
/// </summary>
public partial class ThriveFeedDisplayer : VBoxContainer
{
    [Export]
    public NodePath? NewsContainerPath;

    [Export]
    public NodePath LoadingIndicatorPath = null!;

#pragma warning disable CA2213
    [Export]
    public Font TitleFont = null!;

    [Export]
    public int TitleFontSize = 20;

    [Export]
    public LabelSettings FooterFontSettings = null!;

    private Container newsContainer = null!;
    private Control loadingIndicator = null!;

    private PackedScene customRichTextScene = null!;
#pragma warning restore CA2213

    private StyleBoxFlat feedItemBackground = null!;

    private Task<IReadOnlyCollection<ThriveNewsFeed.FeedItem>>? newsTask;

    private IEnumerator<ThriveNewsFeed.FeedItem>? newsEnumerator;

    private bool itemsCreated;

    public override void _Ready()
    {
        newsContainer = GetNode<Container>(NewsContainerPath);
        loadingIndicator = GetNode<Control>(LoadingIndicatorPath);

        customRichTextScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        feedItemBackground = new StyleBoxFlat
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

        // This doesn't check the returning to menu as if we have downloaded the data twice, we don't do that again
        // and if we should be hidden we'll need the logic to run to hide ourselves anyway

        // Start feed fetch here as early as possible to not make the user wait a long time after the menu is
        // visible to see it
        CheckStartFetchNews();
    }

    public override void _Process(double delta)
    {
        if (newsEnumerator != null)
        {
            // Process one item per frame to avoid huge lag spikes
            while (newsEnumerator.MoveNext())
            {
                var feedItem = newsEnumerator.Current;

                CreateFeedItemGUI(feedItem);

                return;
            }

            // All processed
            newsEnumerator.Dispose();
            newsEnumerator = null;
            return;
        }

        if (newsTask == null)
            return;

        if (!newsTask.IsCompleted)
            return;

        // News are now ready for showing
        newsEnumerator = newsTask.Result.GetEnumerator();
        newsTask = null;

        newsContainer.QueueFreeChildren();
        loadingIndicator.Visible = false;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            // Rebuild the displayed data if we have data currently
            // As the data is cached by the fetcher, we can very cheaply just start the fetch task again
            // TODO: would be nice to only recreate the data once the user is done picking their language and exits
            // the menu
            if (itemsCreated)
            {
                CheckStartFetchNews(true);
            }
        }
    }

    public void CheckStartFetchNews(bool redoIfReady = false)
    {
        if (Settings.Instance.ThriveNewsFeedEnabled)
        {
            if (!redoIfReady && itemsCreated)
                return;

            if (newsTask != null)
                return;

            Visible = true;

            newsTask = ThriveNewsFeed.GetFeedContents();
            loadingIndicator.Visible = true;
            itemsCreated = false;
        }
        else
        {
            Visible = false;
            newsContainer.QueueFreeChildren();
            itemsCreated = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NewsContainerPath != null)
            {
                NewsContainerPath.Dispose();
                LoadingIndicatorPath.Dispose();
                feedItemBackground.Dispose();
            }

            newsEnumerator?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CreateFeedItemGUI(ThriveNewsFeed.FeedItem feedItem)
    {
        var itemContainer = new PanelContainer();

        // Customize the feed item background style to be less visible to not make the main menu look too busy
        itemContainer.AddThemeStyleboxOverride("panel", feedItemBackground);

        var itemContentContainer = new VBoxContainer();
        itemContainer.AddChild(itemContentContainer);

        string titleText;

        if (feedItem.ReadLink != null)
        {
            titleText = $"{Constants.CLICKABLE_TEXT_BBCODE}" +
                $"[url={feedItem.ReadLink}]{feedItem.Title}[/url]{Constants.CLICKABLE_TEXT_BBCODE_END}";
        }
        else
        {
            titleText = feedItem.Title;
        }

        // This uses rich text purely to be clickable
        var title = customRichTextScene.Instantiate<CustomRichTextLabel>();

        // We don't generate custom bbcode when converting html so we use the simpler form here
        // but we need to use the custom rich text label to ensure the links are clickable
        title.Text = titleText;

        // Big font for titles
        title.AddThemeFontOverride("normal_font", TitleFont);
        title.AddThemeFontSizeOverride("normal_font_size", TitleFontSize);

        itemContentContainer.AddChild(title);

        var textDisplayer = customRichTextScene.Instantiate<CustomRichTextLabel>();

        // Make the feed look nicer with less repeating content by stripping the last part of the text
        var content = Constants.NewsFeedRegexDeleteContent.Replace(feedItem.ContentBbCode, "\n");

        textDisplayer.Text = content;

        itemContentContainer.AddChild(textDisplayer);

        var footerText = feedItem.GetFooterText();

        if (!string.IsNullOrWhiteSpace(footerText))
        {
            var footerLabel = new Label
            {
                Text = footerText,
            };

            footerLabel.LabelSettings = FooterFontSettings;

            itemContentContainer.AddChild(footerLabel);
        }

        newsContainer.AddChild(itemContainer);
        itemsCreated = true;
    }
}
