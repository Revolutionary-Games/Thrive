using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class GalleryViewer : CustomDialog
{
    public const string ALL_CATEGORY = "All";

    [Export]
    public NodePath GalleryGridPath = null!;

    [Export]
    public NodePath TabButtonsContainerPath = null!;

    [Export]
    public NodePath AssetsCategoryDropdownPath = null!;

    [Export]
    public NodePath SlideshowButtonPath = null!;

    [Export]
    public PackedScene GalleryCardScene = null!;

    [Export]
    public PackedScene GalleryCardModelScene = null!;

    [Export]
    public PackedScene GalleryCardAudioScene = null!;

    [Export]
    public PackedScene GalleryDetailsToolTipScene = null!;

    // TODO: Replace GridContainer with FlowContainer https://github.com/godotengine/godot/pull/57960
    private GridContainer cardTile = null!;

    private HBoxContainer tabButtonsContainer = null!;
    private OptionButton assetsCategoryDropdown = null!;
    private SlideScreen slideScreen = null!;
    private Button slideshowButton = null!;

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
        tabButtonsContainer = GetNode<HBoxContainer>(TabButtonsContainerPath);
        assetsCategoryDropdown = GetNode<OptionButton>(AssetsCategoryDropdownPath);
        slideshowButton = GetNode<Button>(SlideshowButtonPath);
    }

    public override void _Process(float delta)
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

    private void InitializeGallery()
    {
        GD.Print("Initializing gallery viewer");

        tabButtonsContainer.QueueFreeChildren();
        cardTile.QueueFreeChildren();

        var tabsButtonGroup = new ButtonGroup();
        var itemsButtonGroup = new ButtonGroup();

        tabsButtonGroup.Connect("pressed", this, nameof(OnGallerySelected));
        itemsButtonGroup.Connect("pressed", this, nameof(OnGalleryItemPressed));

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
            tabButtonsContainer.AddChild(tabButton);

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

        firstEntry!.Pressed = true;

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
            case AssetType.Texture:
                item = GalleryCardScene.Instance<GalleryCard>();
                item.Thumbnail = GD.Load<Texture>(asset.ResourcePath);
                break;
            case AssetType.ModelScene:
                item = GalleryCardModelScene.Instance<GalleryCardModel>();
                break;
            case AssetType.AudioPlayback:
                item = GalleryCardAudioScene.Instance<GalleryCardAudio>();
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
        item.Connect(nameof(GalleryCard.OnFullscreenView), this, nameof(OnAssetPreviewOpened));

        var tooltip = GalleryDetailsToolTipScene.Instance<GalleryDetailsTooltip>();
        tooltip.Name = "galleryCard_" + asset.ResourcePath.GetFile();
        tooltip.DisplayName = asset.Title;
        tooltip.Description = asset.Description;
        tooltip.Artist = asset.Artist;
        item.RegisterToolTipForControl(tooltip);
        ToolTipManager.Instance.AddToolTip(tooltip, "artGallery");

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
        slideScreen.CustomShow();
    }

    private void OnGalleryItemPressed(GalleryCard item)
    {
        if (CurrentCards == null)
            return;

        if (lastSelected == item)
        {
            lastSelected = null;
            item.Pressed = false;
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

        if (selected == currentGallery)
            return;

        var gallery = SimulationParameters.Instance.GetGallery(selected);

        currentGallery = selected;
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
        slideScreen.CustomShow();
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
}
