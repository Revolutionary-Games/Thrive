using System.Collections.Generic;
using Godot;

public class GalleryViewer : CustomDialog
{
    [Export]
    public NodePath GalleryGridPath = null!;

    [Export]
    public NodePath TabButtonsContainerPath = null!;

    [Export]
    public NodePath AssetsCategoryDropdownPath = null!;

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
    private Slidescreen slidescreen = null!;

    private Dictionary<string, Dictionary<int, string>> galleries = new();
    private List<GalleryCard> galleryCards = new();
    private string currentGallery = string.Empty;
    private int previouslySelectedAssetsCategory;

    private ButtonGroup? buttonGroup;
    private GalleryCard? lastSelected;

    public override void _Ready()
    {
        slidescreen = GetNode<Slidescreen>("Slidescreen");
        cardTile = GetNode<GridContainer>(GalleryGridPath);
        tabButtonsContainer = GetNode<HBoxContainer>(TabButtonsContainerPath);
        assetsCategoryDropdown = GetNode<OptionButton>(AssetsCategoryDropdownPath);

        InitializeGallery();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
            InitializeGallery();
    }

    public void UpdateGalleryTile(string selectedCategory = "all")
    {
        cardTile.FreeChildren();
        galleryCards.Clear();

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGalleryItemPressed));

        var gallery = SimulationParameters.Instance.GetGallery(currentGallery);

        foreach (var category in gallery.AssetCategories)
        {
            if (selectedCategory != "all" && selectedCategory != category.Key)
                continue;

            foreach (var asset in category.Value.Assets)
            {
                var item = CreateGalleryItem(asset);
                cardTile.AddChild(item);
                galleryCards.Add(item);
            }
        }

        slidescreen.Items = galleryCards;
    }

    private GalleryCard CreateGalleryItem(Asset asset)
    {
        GalleryCard item = null!;

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
        }

        item.Asset = asset;
        item.Group = buttonGroup;
        item.Connect(nameof(GalleryCard.OnFullscreenView), this, nameof(OnAssetPreviewOpened));

        var tooltip = GalleryDetailsToolTipScene.Instance<GalleryDetailsTooltip>();
        tooltip.Name = "galleryCard_" + asset.ResourcePath.GetFile();
        tooltip.DisplayName = asset.Title;
        tooltip.Description = asset.Description;
        tooltip.Artist = asset.Artist!;
        item.RegisterToolTipForControl(tooltip);
        ToolTipManager.Instance.AddToolTip(tooltip, "artGallery");

        return item;
    }

    private void InitializeGallery()
    {
        galleries.Clear();
        tabButtonsContainer.QueueFreeChildren();

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGallerySelected));

        Button firstEntry = null!;

        foreach (var gallery in SimulationParameters.Instance.GetGalleries())
        {
            galleries[gallery.Key] = new Dictionary<int, string>();

            var categories = galleries[gallery.Key];
            var id = 0;

            categories.Add(id, "all");

            var tabButton = new Button
            {
                Name = gallery.Key,
                Text = gallery.Value.Name,
                SizeFlagsHorizontal = 0,
                ToggleMode = true,
                ActionMode = BaseButton.ActionModeEnum.Press,
                Group = buttonGroup,
            };

            firstEntry ??= tabButton;
            tabButtonsContainer.AddChild(tabButton);

            foreach (var category in gallery.Value.AssetCategories)
            {
                ++id;
                categories.Add(id, category.Key);
            }
        }

        firstEntry.Pressed = true;
    }

    private void OnAssetPreviewOpened(GalleryCard item)
    {
        slidescreen.CurrentSlideIndex = galleryCards.IndexOf(item);
        slidescreen.CustomShow();
    }

    private void OnGalleryItemPressed(GalleryCard item)
    {
        if (lastSelected == item)
        {
            lastSelected = null;
            item.Pressed = false;
        }
        else
        {
            lastSelected = item;
            slidescreen.CurrentSlideIndex = galleryCards.IndexOf(item);
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
            if (entry.Key == 0 && entry.Value == "all")
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
        if (index == previouslySelectedAssetsCategory)
            return;

        if (galleries[currentGallery].TryGetValue(index, out string category))
        {
            UpdateGalleryTile(category);
            previouslySelectedAssetsCategory = index;
        }
    }

    private void OnStartSlideshowButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        slidescreen.CurrentSlideIndex = 0;
        slidescreen.SlideshowMode = true;
        slidescreen.CustomShow();
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
