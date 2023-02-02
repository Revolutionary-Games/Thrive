using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class ThriveFeedDisplayer : VBoxContainer
{
    [Export]
    public NodePath? NewsContainerPath;

    [Export]
    public NodePath LoadingIndicatorPath = null!;

#pragma warning disable CA2213
    private Container newsContainer = null!;
    private Control loadingIndicator = null!;

    private PackedScene customRichTextScene = null!;
#pragma warning restore CA2213

    private Task<IReadOnlyCollection<ThriveNewsFeed.FeedItem>>? newsTask;

    private IEnumerator<ThriveNewsFeed.FeedItem>? newsEnumerator;

    private bool processingFirstNewsItem;

    public override void _Ready()
    {
        newsContainer = GetNode<Container>(NewsContainerPath);
        loadingIndicator = GetNode<Control>(LoadingIndicatorPath);

        customRichTextScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        // This doesn't check the returning to menu as if we have downloaded the data twice, we don't do that again
        // and if we should be hidden we'll need the logic to run to hide ourselves anyway

        // Start feed fetch here as early as possible to not make the user wait a long time after the menu is
        // visible to see it
        CheckStartFetchNews();
    }

    public override void _Process(float delta)
    {
        if (newsEnumerator != null)
        {
            // Process one item per frame to avoid huge lag spikes
            while (newsEnumerator.MoveNext())
            {
                var feedItem = newsEnumerator.Current;

                if (feedItem == null)
                {
                    GD.PrintErr("Feed item enumerator item is unexpectedly null");
                    continue;
                }

                var itemContainer = new VBoxContainer();

                if (processingFirstNewsItem)
                    itemContainer.MarginTop = 10;

                processingFirstNewsItem = false;

                // TODO: big font
                // TODO: make into a clickable button
                var title = new Label
                {
                    Text = feedItem.Title,
                };

                // feedItem.ReadLink

                itemContainer.AddChild(title);

                var textDisplayer = customRichTextScene.Instance<CustomRichTextLabel>();

                textDisplayer.ExtendedBbcode = feedItem.ContentBbCode;

                itemContainer.AddChild(textDisplayer);

                if (!string.IsNullOrWhiteSpace(feedItem.FooterLine))
                {
                    var footerText = new Label
                    {
                        Text = feedItem.FooterLine,
                    };

                    itemContainer.AddChild(footerText);
                }

                newsContainer.AddChild(itemContainer);

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
        processingFirstNewsItem = true;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            // Rebuild the displayed data if we have data currently
            // As the data is cached by the fetcher, we can very cheaply just start the fetch task again
            if (newsTask == null)
                CheckStartFetchNews();
        }
    }

    public void CheckStartFetchNews()
    {
        if (Settings.Instance.ThriveNewsFeedEnabled)
        {
            Visible = true;

            if (newsTask != null)
                return;

            newsTask = ThriveNewsFeed.GetFeedContents();
            loadingIndicator.Visible = true;
        }
        else
        {
            Visible = false;
            newsContainer.QueueFreeChildren();
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
            }

            newsEnumerator?.Dispose();
        }

        base.Dispose(disposing);
    }
}
