using System;
using System.Collections.Generic;
using Godot;
using Asset = Gallery.Asset;

public class GalleryViewer : CustomDialog
{
    public const float SLIDESHOW_INTERVAL = 3f;
    public const float TOOLBAR_DISPLAY_DURATION = 2f;

    [Export]
    public NodePath GalleryGridPath;

    [Export]
    public PackedScene GalleryItemScene;

    [Export]
    public NodePath DetailsPanelPath;

    [Export]
    public NodePath SlideTextureRectPath;

    [Export]
    public NodePath SlideToolbarPath;

    [Export]
    public NodePath SlideNextButtonPath;

    [Export]
    public NodePath SlidePrevButtonPath;

    [Export]
    public NodePath SlideCloseButtonPath;

    [Export]
    public NodePath SlideShowModeButtonPath;

    [Export]
    public NodePath DetailsTitleLabelPath;

    [Export]
    public NodePath DetailsDescriptionLabelPath;

    [Export]
    public NodePath DetailsArtistLabelPath;

    [Export]
    public Category ShownCategory = Category.ConceptArt;

    private PanelContainer detailsPanel;
    private GridContainer gridContainer;
    private Popup fullscreenView;
    private TextureRect fullscreenRect;
    private Control slideToolbar;
    private Button slideNextButton;
    private Button slidePrevButton;
    private Button slideCloseButton;
    private Button slideShowModeButton;

    private Label detailsTitleLabel;
    private Label detailsDescriptionLabel;
    private Label detailsArtistLabel;

    private List<Asset> galleryAssets = new();

    private int currentSlideIndex;

    private bool slideshowEnabled;
    private float slideshowTimer;

    private float toolbarHideTimer;
    private bool toolbarHovered;

    private ButtonGroup buttonGroup;
    private GalleryItem lastSelected;

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
        base._EnterTree();
        InputManager.UnregisterReceiver(this);
    }

    public override void _Ready()
    {
        fullscreenView = GetNode<Popup>("SlideView");
        detailsPanel = GetNode<PanelContainer>(DetailsPanelPath);
        gridContainer = GetNode<GridContainer>(GalleryGridPath);
        fullscreenRect = GetNode<TextureRect>(SlideTextureRectPath);
        slideToolbar = GetNode<Control>(SlideToolbarPath);
        slideNextButton = GetNode<Button>(SlideNextButtonPath);
        slidePrevButton = GetNode<Button>(SlidePrevButtonPath);
        slideCloseButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);
        detailsTitleLabel = GetNode<Label>(DetailsTitleLabelPath);
        detailsDescriptionLabel = GetNode<Label>(DetailsDescriptionLabelPath);
        detailsArtistLabel = GetNode<Label>(DetailsArtistLabelPath);

        UpdateGalleryTile();
    }

    public override void _Process(float delta)
    {
        if (toolbarHideTimer >= 0 && !toolbarHovered)
        {
            toolbarHideTimer -= delta;

            if (slideToolbar.Modulate.a < 1)
            {
                GUICommon.Instance.Tween.InterpolateProperty(slideToolbar, "modulate:a", null, 1, 0.5f);
                GUICommon.Instance.Tween.Start();
            }

            if (toolbarHideTimer < 0)
            {
                GUICommon.Instance.Tween.InterpolateProperty(
                    slideToolbar, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut, 2);
                GUICommon.Instance.Tween.Start();
            }
        }

        if (slideshowTimer >= 0 && slideshowEnabled)
        {
            slideshowTimer -= delta;

            if (slideshowTimer < 0)
                AdvanceSlide();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
            toolbarHideTimer = TOOLBAR_DISPLAY_DURATION;
    }

    public void UpdateGalleryTile()
    {
        gridContainer.FreeChildren();
        galleryAssets.Clear();

        buttonGroup = new ButtonGroup();
        buttonGroup.Connect("pressed", this, nameof(OnGalleryItemSelected));

        var gallery = SimulationParameters.Instance.GetGallery(ShownCategory.ToString());

        foreach (var category in gallery.Assets)
        {
            foreach (var asset in category.Value)
            {
                var box = GalleryItemScene.Instance<GalleryItem>();
                box.Asset = asset;
                box.Group = buttonGroup;

                box.OnFullscreenView += new EventHandler<GalleryItemSelectedCallbackData>(OnAssetPreviewOpened);

                gridContainer.AddChild(box);
                galleryAssets.Add(box.Asset);
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

    [RunOnKeyDownWithRepeat("ui_right")]
    public void AdvanceSlide()
    {
        if (currentSlideIndex >= galleryAssets.Count - 1)
        {
            currentSlideIndex = galleryAssets.Count - 1;
            StopSlideShow();
            return;
        }

        currentSlideIndex++;

        UpdateSlide();

        if (!fullscreenView.Visible)
            fullscreenView.Popup_();

        if (slideshowEnabled)
            slideshowTimer = 2;

        UpdateNextPrevButton();
    }

    [RunOnKeyDownWithRepeat("ui_left")]
    public void RetreatSlide()
    {
        if (currentSlideIndex <= 0)
        {
            currentSlideIndex = 0;
            return;
        }

        currentSlideIndex--;

        UpdateSlide();

        if (!fullscreenView.Visible)
            fullscreenView.Popup_();

        UpdateNextPrevButton();
    }

    private void UpdateSlide()
    {
        var slide = galleryAssets[currentSlideIndex];

        fullscreenRect.Texture = GD.Load(slide.ResourcePath) as Texture;

        var noneText = TranslationServer.Translate("N_A");

        detailsTitleLabel.Text = string.IsNullOrEmpty(slide.Title) ? noneText : slide.Title;
        detailsDescriptionLabel.Text = string.IsNullOrEmpty(slide.Description) ? noneText : slide.Description;
        detailsArtistLabel.Text = string.IsNullOrEmpty(slide.Artist) ? noneText : slide.Artist;
    }

    private void UpdateNextPrevButton()
    {
        slideNextButton.Disabled = currentSlideIndex >= galleryAssets.Count - 1;
        slidePrevButton.Disabled = currentSlideIndex <= 0;
    }

    private void OnAssetPreviewOpened(object sender, GalleryItemSelectedCallbackData data)
    {
        currentSlideIndex = galleryAssets.IndexOf(data.Asset);
        UpdateSlide();

        fullscreenView.Popup_();

        UpdateNextPrevButton();
    }

    private void OnGalleryItemSelected(GalleryItem item)
    {
        if (lastSelected == item)
        {
            lastSelected = null;
            item.Pressed = false;
            detailsPanel.Hide();

        }
        else
        {
            lastSelected = item;
            currentSlideIndex = galleryAssets.IndexOf(item.Asset);
            detailsPanel.Show();
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

        fullscreenView.Popup_();
    }

    private void OnSlideCloseButtonPressed()
    {
        fullscreenView.Hide();
    }

    private void OnSlideHidden()
    {
        StopSlideShow();
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
