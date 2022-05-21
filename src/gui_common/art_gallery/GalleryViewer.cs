using System;
using System.Collections.Generic;
using Godot;

public class GalleryViewer : CustomDialog
{
    [Export]
    public NodePath GalleryGridPath = null!;

    [Export]
    public PackedScene GalleryItemScene = null!;

    [Export]
    public PackedScene GalleryDetailsToolTipScene = null!;

    [Export]
    public Category ShownCategory = Category.ConceptArt;

    // TODO: Replace GridContainer with FlowContainer https://github.com/godotengine/godot/pull/57960
    private GridContainer gridContainer = null!;

    private SlideshowScreen slideshowScreen = null!;

    private List<GalleryItem> galleryItems = new();

    private ButtonGroup? buttonGroup;
    private GalleryItem? lastSelected;

    public enum Category
    {
        ConceptArt,
        Models,
    }

    public override void _Ready()
    {
        slideshowScreen = GetNode<SlideshowScreen>("SlideshowScreen");
        gridContainer = GetNode<GridContainer>(GalleryGridPath);

        UpdateGalleryTile();
    }

    public void UpdateGalleryTile()
    {
        gridContainer.FreeChildren();
        galleryItems.Clear();

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGalleryItemPressed));

        var gallery = SimulationParameters.Instance.GetGallery(ShownCategory.ToString());

        foreach (var category in gallery.Assets)
        {
            foreach (var asset in category.Value)
            {
                var box = GalleryItemScene.Instance<GalleryItem>();
                box.Asset = asset;
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
