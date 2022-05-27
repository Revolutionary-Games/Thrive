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
    public PackedScene GalleryItemScene = null!;

    [Export]
    public PackedScene GalleryDetailsToolTipScene = null!;

    // TODO: Replace GridContainer with FlowContainer https://github.com/godotengine/godot/pull/57960
    private GridContainer gridContainer = null!;

    private HBoxContainer tabButtonsContainer = null!;
    private OptionButton assetsCategoryDropdown = null!;
    private SlideshowScreen slideshowScreen = null!;

    private Dictionary<string, Dictionary<int, string>> galleries = new();
    private List<GalleryItem> galleryItems = new();
    private string currentGallery = string.Empty;
    private int previouslySelectedAssetsCategory;

    private ButtonGroup? buttonGroup;
    private GalleryItem? lastSelected;

    public override void _Ready()
    {
        slideshowScreen = GetNode<SlideshowScreen>("SlideshowScreen");
        gridContainer = GetNode<GridContainer>(GalleryGridPath);
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
        gridContainer.FreeChildren();
        galleryItems.Clear();

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGalleryItemPressed));

        var gallery = SimulationParameters.Instance.GetGallery(currentGallery);

        foreach (var category in gallery.AssetCategories)
        {
            if (selectedCategory != "all" && selectedCategory != category.Key)
                continue;

            foreach (var asset in category.Value.Assets)
            {
                var box = GalleryItemScene.Instance<GalleryItem>();
                box.Title = asset.Title!;
                box.Image = GD.Load<Texture>(asset.ResourcePath);
                box.Group = buttonGroup;
                box.Connect(nameof(GalleryItem.OnFullscreenView), this, nameof(OnAssetPreviewOpened));

                var tooltip = GalleryDetailsToolTipScene.Instance<GalleryDetailsTooltip>();
                tooltip.Name = "galleryItem_" + asset.ResourcePath.GetFile();
                tooltip.DisplayName = asset.Title!;
                tooltip.Description = asset.Description;
                tooltip.Artist = asset.Artist!;
                box.RegisterToolTipForControl(tooltip);
                ToolTipManager.Instance.AddToolTip(tooltip, "artGallery");

                gridContainer.AddChild(box);
                galleryItems.Add(box);
            }
        }

        slideshowScreen.SlideItems = galleryItems;
    }

    private void InitializeGallery()
    {
        galleries.Clear();
        tabButtonsContainer.QueueFreeChildren();

        var id = 0;
        Button firstEntry = null!;

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGallerySelected));

        foreach (var gallery in SimulationParameters.Instance.GetGalleries())
        {
            galleries[gallery.Key] = new Dictionary<int, string>();

            var categories = galleries[gallery.Key];
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

    private void OnAssetPreviewOpened(GalleryItem item)
    {
        slideshowScreen.CurrentSlideIndex = galleryItems.IndexOf(item);
        slideshowScreen.CustomShow();
    }

    private void OnGalleryItemPressed(GalleryItem item)
    {
        if (lastSelected == item)
        {
            lastSelected = null;
            item.Pressed = false;
        }
        else
        {
            lastSelected = item;
            slideshowScreen.CurrentSlideIndex = galleryItems.IndexOf(item);
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

        slideshowScreen.CurrentSlideIndex = 0;
        slideshowScreen.SlideshowMode = true;
        slideshowScreen.CustomShow();
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
