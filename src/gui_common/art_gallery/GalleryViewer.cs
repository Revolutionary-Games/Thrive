using System;
using System.Collections.Generic;
using Godot;
using Asset = Gallery.Asset;
using Object = Godot.Object;

public class GalleryViewer : CustomDialog
{
    public const float SLIDESHOW_INTERVAL = 6f;
    public const float TOOLBAR_DISPLAY_DURATION = 4f;

    [Export]
    public NodePath GalleryGridPath = null!;

    [Export]
    public PackedScene GalleryItemScene = null!;

    [Export]
    public NodePath SlideTextureRectPath = null!;

    [Export]
    public NodePath SlideToolbarPath = null!;

    [Export]
    public NodePath SlideNextButtonPath = null!;

    [Export]
    public NodePath SlidePrevButtonPath = null!;

    [Export]
    public NodePath SlideCloseButtonPath = null!;

    [Export]
    public NodePath SlideShowModeButtonPath = null!;

    [Export]
    public PackedScene GalleryDetailsToolTipScene = null!;

    [Export]
    public Category ShownCategory = Category.ConceptArt;

    private GridContainer gridContainer = null!;
    private SlideshowScreen slideshowScreen = null!;
    private TextureRect fullscreenRect = null!;
    private Control slideToolbar = null!;
    private Button slideNextButton = null!;
    private Button slidePrevButton = null!;
    private Button slideCloseButton = null!;
    private Button slideShowModeButton = null!;
    private Tween localTween = null!;

    private List<GalleryItem> galleryItems = new();

    private int currentSlideIndex;

    private bool slideshowEnabled;
    private float slideshowTimer;

    private float toolbarHideTimer;
    private bool toolbarHovered;

    private ButtonGroup? buttonGroup;
    private GalleryItem? lastSelected;

    public enum Category
    {
        ConceptArt,
        Models,
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.UnregisterReceiver(this);
    }

    public override void _Ready()
    {
        slideshowScreen = GetNode<SlideshowScreen>("SlideshowScreen");
        localTween = GetNode<Tween>("Tween");
        gridContainer = GetNode<GridContainer>(GalleryGridPath);
        fullscreenRect = GetNode<TextureRect>(SlideTextureRectPath);
        slideToolbar = GetNode<Control>(SlideToolbarPath);
        slideNextButton = GetNode<Button>(SlideNextButtonPath);
        slidePrevButton = GetNode<Button>(SlidePrevButtonPath);
        slideCloseButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);

        UpdateGalleryTile();
    }

    public override void _Process(float delta)
    {
        if (toolbarHideTimer >= 0 && !toolbarHovered)
        {
            toolbarHideTimer -= delta;

            var guiCommon = GUICommon.Instance;

            if (slideToolbar.Modulate.a < 1)
            {
                guiCommon.Tween.InterpolateProperty(slideToolbar, "modulate:a", null, 1, 0.5f);
                guiCommon.Tween.InterpolateProperty(slideCloseButton, "modulate:a", null, 1, 0.5f);
                guiCommon.Tween.Start();
            }

            if (Input.GetMouseMode() == Input.MouseMode.Hidden)
                Input.SetMouseMode(Input.MouseMode.Visible);

            if (toolbarHideTimer < 0)
            {
                guiCommon.Tween.InterpolateProperty(
                    slideToolbar, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                guiCommon.Tween.InterpolateProperty(slideCloseButton, "modulate:a", null, 0, 0.5f,
                    Tween.TransitionType.Linear, Tween.EaseType.InOut);
                guiCommon.Tween.Start();

                Input.SetMouseMode(Input.MouseMode.Hidden);
            }
        }

        if (slideshowTimer >= 0 && slideshowEnabled)
        {
            slideshowTimer -= delta;

            if (slideshowTimer < 0)
            {
                AdvanceSlide(true);
                slideshowTimer = SLIDESHOW_INTERVAL;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
            toolbarHideTimer = TOOLBAR_DISPLAY_DURATION;
    }

    [RunOnKeyDownWithRepeat("ui_right")]
    public void OnSlideToRight()
    {
        AdvanceSlide();
    }

    [RunOnKeyDownWithRepeat("ui_left")]
    public void OnSlideToLeft()
    {
        RetreatSlide();
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
                box.OnFullscreenView += new EventHandler<GalleryItemSelectedCallbackData>(OnAssetPreviewOpened);

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
    }

    public void StartSlideShow()
    {
        slideshowEnabled = true;
        slideshowTimer = SLIDESHOW_INTERVAL;
        slideShowModeButton.SetPressedNoSignal(true);
    }

    public void StopSlideShow()
    {
        slideshowEnabled = false;
        slideshowTimer = 0;
        slideShowModeButton.SetPressedNoSignal(false);
    }

    
    public void AdvanceSlide(bool fade = false)
    {
        currentSlideIndex = (currentSlideIndex + 1) % galleryItems.Count;

        ChangeSlide(fade);

        if (!slideshowScreen.Visible)
            slideshowScreen.CustomShow();
    }

    public void RetreatSlide(bool fade = false)
    {
        --currentSlideIndex;

        if (currentSlideIndex < 0)
            currentSlideIndex = galleryItems.Count - 1;

        ChangeSlide(fade);

        if (!slideshowScreen.Visible)
            slideshowScreen.CustomShow();
    }

    private void UpdateSlide()
    {
        var slide = galleryItems[currentSlideIndex];
        fullscreenRect.Texture = GD.Load(slide.Asset.ResourcePath) as Texture;
        slideshowScreen.FocusedItem = slide;
    }

    private void ChangeSlide(bool fade)
    {
        if (!fade)
        {
            UpdateSlide();
            return;
        }

        localTween.InterpolateProperty(fullscreenRect, "modulate", null, Colors.Black, 0.5f);
        localTween.Start();

        if (!localTween.IsConnected("tween_completed", this, nameof(OnSlideFaded)))
            localTween.Connect("tween_completed", this, nameof(OnSlideFaded), null, (uint)ConnectFlags.Oneshot);
    }

    private void OnSlideFaded(Object @object, NodePath key)
    {
        UpdateSlide();
        localTween.InterpolateProperty(fullscreenRect, "modulate", null, Colors.White, 0.5f);
        localTween.Start();
    }

    private void OnAssetPreviewOpened(object sender, GalleryItemSelectedCallbackData data)
    {
        // TODO: Clean this up
        _ = data;

        var item = (GalleryItem)sender;

        currentSlideIndex = galleryItems.IndexOf(item);
        UpdateSlide();

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
            currentSlideIndex = galleryItems.IndexOf(item);
            UpdateSlide();
        }
    }

    private void OnNextSlideButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        AdvanceSlide();
    }

    private void OnPreviousSlideButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        RetreatSlide();
    }

    private void OnSlideshowModeButtonToggled(bool pressed)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (pressed)
        {
            StartSlideShow();
        }
        else
        {
            StopSlideShow();
        }
    }

    private void OnStartSlideshowButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        currentSlideIndex = 0;
        UpdateSlide();
        StartSlideShow();

        slideshowScreen.CustomShow();
    }

    private void OnSlideCloseButtonPressed()
    {
        slideshowScreen.CustomHide();
    }

    private void OnSlideHidden()
    {
        StopSlideShow();
        Input.SetMouseMode(Input.MouseMode.Visible);
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
