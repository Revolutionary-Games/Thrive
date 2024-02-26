using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GalleryViewer : CustomWindow
{
    public const string ALL_CATEGORY = "All";

    [Export]
    public NodePath? GalleryGridPath;

    [Export]
    public NodePath TabButtonsPath = null!;

    [Export]
    public NodePath AssetsCategoryDropdownPath = null!;

    [Export]
    public NodePath SlideshowButtonPath = null!;

#pragma warning disable CA2213
    [Export]
    public PackedScene GalleryCardScene = null!;

    [Export]
    public PackedScene GalleryCardModelScene = null!;

    [Export]
    public PackedScene GalleryCardAudioScene = null!;

    [Export]
    public PackedScene GalleryDetailsToolTipScene = null!;

    private readonly List<(Control Control, ICustomToolTip ToolTip)> registeredToolTips = new();

    // TODO: Replace GridContainer with FlowContainer https://github.com/godotengine/godot/pull/57960
    private GridContainer cardTile = null!;

    private TabButtons tabButtons = null!;
    private OptionButton assetsCategoryDropdown = null!;
    private SlideScreen slideScreen = null!;
    private Button slideshowButton = null!;
#pragma warning restore CA2213

    private bool tooltipsDetached;

    /// <summary>
    ///   Holds gallery categories and their respective asset categories with their respective indexes in the category
    ///   dropdown.
    /// </summary>
    private Dictionary<string, Dictionary<int, string>> galleries = new();

    /// <summary>
    ///   The gallery cards sorted by gallery category and asset category respectively. Organized like such for easier
    ///   caching, trade off: nested loops.
    /// </summary>
    private Dictionary<string, Dictionary<string, List<GalleryCard>>> categoryCards = new();

    private string? currentGallery;
    private string currentCategory = ALL_CATEGORY;

    private int previouslySelectedAssetCategoryIndex;
    private bool hasBecomeVisibleAtLeastOnce;
    private int activeAudioPlayers;

    private bool initialized;

    private GalleryCard? lastSelected;

    /// <summary>
    ///   List of gallery cards based on the current gallery and asset category.
    /// </summary>
    public List<GalleryCard>? CurrentCards
    {
        get
        {
            if (!categoryCards.ContainsKey(currentGallery!))
                return null;

            if (!categoryCards[currentGallery!].ContainsKey(currentCategory))
                return null;

            return categoryCards[currentGallery!][currentCategory];
        }
    }

    public override void _Ready()
    {
        slideScreen = GetNode<SlideScreen>("SlideScreen");
        cardTile = GetNode<GridContainer>(GalleryGridPath);
        tabButtons = GetNode<TabButtons>(TabButtonsPath);
        assetsCategoryDropdown = GetNode<OptionButton>(AssetsCategoryDropdownPath);
        slideshowButton = GetNode<Button>(SlideshowButtonPath);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (tooltipsDetached)
            ReAttachToolTips();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (registeredToolTips.Count > 0)
            DetachToolTips();

        UnregisterToolTips();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        // Actively checking for active audio players every frame eliminate the possibility of Jukebox being
        // stuck when pausing or resuming
        if (activeAudioPlayers > 0)
        {
            Jukebox.Instance.Pause(true);
        }
        else
        {
            Jukebox.Instance.Resume(true);
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged && hasBecomeVisibleAtLeastOnce)
        {
            InitializeGallery();
        }
        else if (what == NotificationVisibilityChanged && Visible && !hasBecomeVisibleAtLeastOnce)
        {
            hasBecomeVisibleAtLeastOnce = true;
            InitializeGallery();
        }
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        StopAllPlayback();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (GalleryGridPath != null)
            {
                GalleryGridPath.Dispose();
                TabButtonsPath.Dispose();
                AssetsCategoryDropdownPath.Dispose();
                SlideshowButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void InitializeGallery()
    {
        GD.Print("Initializing gallery viewer");

        tabButtons.ClearTabButtons();
        cardTile.QueueFreeChildren();
        UnregisterToolTips();

        var tabsButtonGroup = new ButtonGroup();
        var itemsButtonGroup = new ButtonGroup();

        tabsButtonGroup.Connect("pressed", new Callable(this, nameof(OnGallerySelected)));
        itemsButtonGroup.Connect("pressed", new Callable(this, nameof(OnGalleryItemPressed)));

        Button? firstEntry = null;

        foreach (var gallery in SimulationParameters.Instance.GetGalleries())
        {
            galleries[gallery.Key] = new Dictionary<int, string>();
            categoryCards[gallery.Key] = new Dictionary<string, List<GalleryCard>>();

            var categories = galleries[gallery.Key];
            var id = 0;

            categories.Add(id, ALL_CATEGORY);

            var tabButton = new Button
            {
                Name = gallery.Key,
                Text = gallery.Value.Name,
                SizeFlagsHorizontal = 0,
                ToggleMode = true,
                ActionMode = BaseButton.ActionModeEnum.Press,
                Group = tabsButtonGroup,
            };

            firstEntry ??= tabButton;
            tabButtons.AddNewTab(tabButton);

            categoryCards[gallery.Key][ALL_CATEGORY] = new List<GalleryCard>();

            foreach (var category in gallery.Value.AssetCategories)
            {
                categoryCards[gallery.Key][category.Key] = new List<GalleryCard>();

                foreach (var asset in category.Value.Assets)
                {
                    var created = CreateGalleryItem(asset, itemsButtonGroup);
                    cardTile.AddChild(created);

                    if (category.Key != ALL_CATEGORY)
                        categoryCards[gallery.Key][ALL_CATEGORY].Add(created);

                    categoryCards[gallery.Key][category.Key].Add(created);
                }

                if (category.Key == ALL_CATEGORY)
                    continue;

                ++id;
                categories.Add(id, category.Key);
            }
        }

        firstEntry!.ButtonPressed = true;

        initialized = true;
    }

    private void UpdateGalleryTile(string selectedCategory = ALL_CATEGORY)
    {
        currentCategory = selectedCategory;

        if (CurrentCards == null)
            return;

        HideAllCards();

        foreach (var card in CurrentCards)
            card.Visible = true;

        slideScreen.CurrentSlideIndex = 0;
        slideScreen.Items = CurrentCards;

        UpdateSlideshowButton();
    }

    private GalleryCard CreateGalleryItem(Asset asset, ButtonGroup buttonGroup)
    {
        GalleryCard item;

        switch (asset.Type)
        {
            case AssetType.Texture2D:
                item = GalleryCardScene.Instantiate<GalleryCard>();

                // To avoid massive lag spikes, only set a placeholder loading icon here and queue a load for the icon
                var resourceManager = ResourceManager.Instance;

                item.Thumbnail = resourceManager.LoadingIcon;

                var loadingResource =
                    new TextureThumbnailResource(asset.ResourcePath, Constants.GALLERY_THUMBNAIL_MAX_WIDTH);

                loadingResource.OnComplete = _ => { item.Thumbnail = loadingResource.LoadedTexture; };

                resourceManager.QueueLoad(loadingResource);
                break;
            case AssetType.ModelScene:
                item = GalleryCardModelScene.Instantiate<GalleryCardModel>();
                break;
            case AssetType.AudioPlayback:
                item = GalleryCardAudioScene.Instantiate<GalleryCardAudio>();
                break;
            default:
                throw new InvalidOperationException("Unhandled asset type: " + asset.Type);
        }

        if (item is IGalleryCardPlayback playback)
        {
            playback.PlaybackStarted += OnPlaybackStarted;
            playback.PlaybackStopped += OnPlaybackStopped;
        }

        item.Asset = asset;
        item.Group = buttonGroup;
        item.Connect(nameof(GalleryCard.OnFullscreenViewEventHandler), new Callable(this, nameof(OnAssetPreviewOpened)));

        // Reuse existing tooltip if possible
        var name = "galleryCard_" + asset.ResourcePath.GetFile();

        var tooltip = ToolTipManager.Instance.GetToolTipIfExists<GalleryDetailsTooltip>(name, "artGallery");

        if (tooltip == null)
        {
            // Need to create a new tooltip
            tooltip = GalleryDetailsToolTipScene.Instantiate<GalleryDetailsTooltip>();
            tooltip.Name = name;
            ToolTipManager.Instance.AddToolTip(tooltip, "artGallery");
        }

        tooltip.DisplayName = string.IsNullOrEmpty(asset.Title) ?
            TranslationServer.Translate("UNTITLED") :
            asset.Title!;
        tooltip.Description = asset.Description;
        tooltip.Artist = asset.Artist;

        item.RegisterToolTipForControl(tooltip, false);
        registeredToolTips.Add((item, tooltip));

        return item;
    }

    private void StopAllPlayback(IGalleryCardPlayback? exception = null)
    {
        foreach (var entry in categoryCards)
        {
            foreach (var items in entry.Value)
            {
                foreach (var item in items.Value)
                {
                    if (item is IGalleryCardPlayback playback && playback.Playing && playback != exception)
                        playback.StopPlayback();
                }
            }
        }
    }

    private void HideAllCards()
    {
        foreach (var entry in categoryCards)
        {
            foreach (var items in entry.Value)
            {
                foreach (var item in items.Value)
                {
                    if (!item.Visible)
                        continue;

                    item.Visible = false;

                    if (item is IGalleryCardPlayback playback)
                        playback.StopPlayback();
                }
            }
        }
    }

    private void UpdateSlideshowButton()
    {
        if (CurrentCards == null)
            return;

        slideshowButton.Disabled = CurrentCards.All(c => !c.CanBeShownInASlideshow);
    }

    private void OnAssetPreviewOpened(GalleryCard item)
    {
        if (CurrentCards == null)
            return;

        slideScreen.CurrentSlideIndex = CurrentCards.IndexOf(item);
        slideScreen.OpenModal();
    }

    private void OnGalleryItemPressed(GalleryCard item)
    {
        if (CurrentCards == null)
            return;

        if (lastSelected == item)
        {
            lastSelected = null;
            item.ButtonPressed = false;
        }
        else
        {
            lastSelected = item;
            slideScreen.CurrentSlideIndex = CurrentCards.IndexOf(item);
        }
    }

    private void OnGallerySelected(Button button)
    {
        var selected = button.Name;

        var gallery = SimulationParameters.Instance.GetGallery(selected);
        assetsCategoryDropdown.Clear();

        foreach (var entry in galleries[selected])
        {
            if (entry.Key == 0 && entry.Value == ALL_CATEGORY)
            {
                assetsCategoryDropdown.AddItem(TranslationServer.Translate("ALL"), entry.Key);
                continue;
            }

            if (gallery.AssetCategories.TryGetValue(entry.Value, out AssetCategory category))
                assetsCategoryDropdown.AddItem(category.Name, entry.Key);
        }

        if (selected == currentGallery)
            return;

        currentGallery = selected;

        UpdateGalleryTile();
    }

    private void OnCategorySelected(int index)
    {
        if (index == previouslySelectedAssetCategoryIndex || string.IsNullOrEmpty(currentGallery))
            return;

        if (galleries[currentGallery!].TryGetValue(index, out string category))
        {
            UpdateGalleryTile(category);
            previouslySelectedAssetCategoryIndex = index;
        }
    }

    private void OnStartSlideshowButtonPressed()
    {
        if (CurrentCards == null)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        slideScreen.CurrentSlideIndex = CurrentCards.FindIndex(c => c.CanBeShownInASlideshow);
        slideScreen.SlideshowMode = true;
        slideScreen.OpenModal();
    }

    private void OnPlaybackStarted(object sender, EventArgs args)
    {
        if (CurrentCards == null)
            return;

        // Assume sender is of playback type, as it should be
        var playback = (IGalleryCardPlayback)sender;

        slideScreen.CurrentSlideIndex = CurrentCards.IndexOf((GalleryCard)playback);
        StopAllPlayback(playback);
        activeAudioPlayers++;
    }

    private void OnPlaybackStopped(object sender, EventArgs args)
    {
        activeAudioPlayers--;

        if (activeAudioPlayers < 0)
        {
            // This being decremented to below zero on init is not a logic error
            if (initialized)
                GD.PrintErr("Active audio player counter is being decremented to below zero");

            activeAudioPlayers = 0;
        }
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }

    private void UnregisterToolTips()
    {
        tooltipsDetached = false;
        if (registeredToolTips.Count < 1)
            return;

        foreach (var (control, tooltip) in registeredToolTips)
        {
            control.UnRegisterToolTipForControl(tooltip);
        }

        registeredToolTips.Clear();
    }

    private void DetachToolTips()
    {
        tooltipsDetached = true;

        foreach (var (control, tooltip) in registeredToolTips)
        {
            control.UnRegisterToolTipForControl(tooltip);
        }
    }

    private void ReAttachToolTips()
    {
        if (!tooltipsDetached)
            return;

        foreach (var (control, tooltip) in registeredToolTips)
        {
            control.RegisterToolTipForControl(tooltip, false);
        }

        tooltipsDetached = false;
    }
}
