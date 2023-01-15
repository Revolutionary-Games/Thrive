﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Screen capable of moving slides of gallery items.
/// </summary>
public class SlideScreen : CustomDialog
{
    public const float SLIDESHOW_INTERVAL = 6.0f;
    public const float TOOLBAR_DISPLAY_DURATION = 4.0f;

    [Export]
    public NodePath SlideTextureRectPath = null!;

    [Export]
    public NodePath SlideToolbarPath = null!;

    [Export]
    public NodePath SlideCloseButtonPath = null!;

    [Export]
    public NodePath SlideShowModeButtonPath = null!;

    [Export]
    public NodePath SlideTitleLabelPath = null!;

    [Export]
    public NodePath ModelViewerContainerPath = null!;

    [Export]
    public NodePath ModelViewerPath = null!;

    [Export]
    public NodePath ModelHolderPath = null!;

    [Export]
    public NodePath ModelViewerCameraPath = null!;

    [Export]
    public NodePath PlaybackControlsPath = null!;

    private CrossFadableTextureRect? slideTextureRect;
    private Control? toolbar;
    private Button? closeButton;
    private Button? slideShowModeButton;
    private Label? slideTitleLabel;
    private ViewportContainer? modelViewerContainer;
    private Viewport? modelViewer;
    private Spatial? modelHolder;
    private OrbitCamera? modelViewerCamera;
    private PlaybackControls? playbackControls;

    private Tween popupTween = null!;
    private Tween toolbarTween = null!;

    private float toolbarHideTimer;
    private float slideshowTimer;

    private List<GalleryCard>? items;
    private int currentSlideIndex;
    private bool slideshowMode;
    private bool slideControlsVisible;

    public List<GalleryCard>? Items
    {
        get => items;
        set
        {
            items = value;
            UpdateScreen();
        }
    }

    public int CurrentSlideIndex
    {
        get => currentSlideIndex;
        set
        {
            currentSlideIndex = value;
            UpdateScreen();
        }
    }

    public bool SlideshowMode
    {
        get => slideshowMode;
        set
        {
            slideshowMode = value;
            UpdateSlide();
        }
    }

    /// <summary>
    ///   Controls means the UI elements on the slideshow screen (e.g next and prev slide button). If false they'll
    ///   be hidden.
    /// </summary>
    public bool SlideControlsVisible
    {
        get => slideControlsVisible;
        set
        {
            slideControlsVisible = value;
            UpdateHandles();
        }
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
        slideTextureRect = GetNode<CrossFadableTextureRect>(SlideTextureRectPath);
        toolbar = GetNode<Control>(SlideToolbarPath);
        closeButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);
        slideTitleLabel = GetNode<Label>(SlideTitleLabelPath);
        modelViewerContainer = GetNode<ViewportContainer>(ModelViewerContainerPath);
        modelViewer = GetNode<Viewport>(ModelViewerPath);
        modelHolder = GetNode<Spatial>(ModelHolderPath);
        modelViewerCamera = GetNode<OrbitCamera>(ModelViewerCameraPath);
        playbackControls = GetNode<PlaybackControls>(PlaybackControlsPath);

        popupTween = GetNode<Tween>("PopupTween");
        toolbarTween = GetNode<Tween>("ToolbarTween");

        UpdateScreen();
    }

    public override void _Process(float delta)
    {
        if (toolbarHideTimer >= 0 && Visible)
        {
            toolbarHideTimer -= delta;

            if (toolbar?.Modulate.a < 1)
            {
                toolbarTween.InterpolateProperty(toolbar, "modulate:a", null, 1, 0.5f);
                toolbarTween.InterpolateProperty(closeButton, "modulate:a", null, 1, 0.5f);
                toolbarTween.Start();
            }

            if (Input.MouseMode == Input.MouseModeEnum.Hidden)
                Input.MouseMode = Input.MouseModeEnum.Visible;

            if (toolbarHideTimer < 0)
            {
                toolbarTween.InterpolateProperty(
                    toolbar, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                toolbarTween.InterpolateProperty(
                    closeButton, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                toolbarTween.Start();
                Input.MouseMode = Input.MouseModeEnum.Hidden;
            }
        }

        if (slideshowTimer >= 0 && slideshowMode)
        {
            slideshowTimer -= delta;

            if (slideshowTimer < 0)
                AdvanceSlide(true);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
            toolbarHideTimer = TOOLBAR_DISPLAY_DURATION;
    }

    [RunOnKeyDownWithRepeat("ui_right", OnlyUnhandled = false)]
    public bool OnSlideToRight()
    {
        if (!Visible)
            return false;

        return AdvanceSlide();
    }

    [RunOnKeyDownWithRepeat("ui_left", OnlyUnhandled = false)]
    public bool OnSlideToLeft()
    {
        if (!Visible)
            return false;

        return RetreatSlide();
    }

    public override void CustomShow()
    {
        if (Items == null)
            return;

        base.CustomShow();

        SlideControlsVisible = false;

        var currentItemRect = Items[currentSlideIndex].GetGlobalRect();
        RectGlobalPosition = currentItemRect.Position;
        RectSize = currentItemRect.Size;

        popupTween.InterpolateProperty(
            this, "rect_position", null, GetFullRect().Position, 0.2f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        popupTween.InterpolateProperty(
            this, "rect_size", null, GetFullRect().Size, 0.2f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        popupTween.Start();

        if (!popupTween.IsConnected("tween_completed", this, nameof(OnScaledUp)))
            popupTween.Connect("tween_completed", this, nameof(OnScaledUp), null, (uint)ConnectFlags.Oneshot);
    }

    public override void CustomHide()
    {
        if (Items == null)
            return;

        FullRect = false;
        SlideControlsVisible = false;
        BoundToScreenArea = false;

        var currentItemRect = Items[currentSlideIndex].GetGlobalRect();

        popupTween.InterpolateProperty(this, "rect_position", null, currentItemRect.Position, 0.2f,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        popupTween.InterpolateProperty(
            this, "rect_size", null, currentItemRect.Size, 0.2f, Tween.TransitionType.Sine,
            Tween.EaseType.Out);
        popupTween.Start();

        if (!popupTween.IsConnected("tween_completed", this, nameof(OnScaledDown)))
            popupTween.Connect("tween_completed", this, nameof(OnScaledDown), null, (uint)ConnectFlags.Oneshot);
    }

    public bool AdvanceSlide(bool fade = false, int searchCounter = 0)
    {
        if (Items == null)
            return false;

        currentSlideIndex = (currentSlideIndex + 1) % Items.Count;

        // Can be shown in a slideshow check is only done here because slideshow only moves forward
        if (!Items[CurrentSlideIndex].CanBeShownInASlideshow && SlideshowMode)
        {
            // TODO: rewrite this to an iterative method, in case we have slideshows with thousands of items
            // Limit recursion to the number of items total
            if (searchCounter < Items.Count)
            {
                // Keep advancing until we found an item that allows slideshow
                return AdvanceSlide(fade, ++searchCounter);
            }

            // Advancing failed
            return false;
        }

        ChangeSlide(fade);
        return true;
    }

    public bool RetreatSlide(bool fade = false)
    {
        if (Items == null)
            return false;

        --currentSlideIndex;

        if (currentSlideIndex < 0)
            currentSlideIndex = Items.Count - 1;

        ChangeSlide(fade);
        return true;
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        SlideshowMode = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void ChangeSlide(bool fade)
    {
        if (!Visible)
            CustomShow();

        if (!fade)
        {
            UpdateScreen();
            return;
        }

        if (items == null || slideTextureRect == null)
            return;

        var item = items[currentSlideIndex];
        slideTextureRect.Image = GD.Load(item.Asset.ResourcePath) as Texture;
    }

    private void UpdateScreen()
    {
        UpdateSlide();
        UpdateModelViewer();
        UpdatePlayback();
    }

    private void UpdateSlide()
    {
        if (items == null || slideTitleLabel == null || slideTextureRect == null || slideShowModeButton == null)
            return;

        var item = items[currentSlideIndex];

        slideshowTimer = slideshowMode ? SLIDESHOW_INTERVAL : 0;
        slideShowModeButton.SetPressedNoSignal(slideshowMode);
        slideShowModeButton.Visible = item.CanBeShownInASlideshow;

        slideTitleLabel.Text = string.IsNullOrEmpty(item.Asset.Title) ? item.Asset.FileName : item.Asset.Title;
        slideTextureRect.Texture = GD.Load(item.Asset.ResourcePath) as Texture;
    }

    private void UpdateModelViewer()
    {
        var item = items?[currentSlideIndex] as GalleryCardModel;

        if (item?.Asset.Type != AssetType.ModelScene || modelHolder == null || modelViewer == null ||
            modelViewerCamera == null)
        {
            modelViewerContainer?.Hide();
            return;
        }

        modelViewerContainer?.Show();
        modelHolder.QueueFreeChildren();

        modelViewer.Msaa = Settings.Instance.MSAAResolution;

        var scene = GD.Load<PackedScene>(item.Asset.ResourcePath);
        var instanced = scene.Instance();

        modelHolder.AddChild(instanced);

        var mesh = instanced.GetNode<MeshInstance>(item.Asset.MeshNodePath!);
        var minDistance = mesh.GetTransformedAabb().Size.Length();
        var maxDistance = PhotoStudio.CameraDistanceFromRadiusOfObject(minDistance);

        modelViewerCamera.MinCameraDistance = minDistance;
        modelViewerCamera.MaxCameraDistance = maxDistance;
        modelViewerCamera.Distance = maxDistance;
    }

    private void UpdatePlayback()
    {
        var item = items?[currentSlideIndex] as GalleryCardAudio;

        if (playbackControls == null || slideTextureRect == null)
            return;

        if (item?.Asset.Type != AssetType.AudioPlayback)
        {
            playbackControls.Hide();
            playbackControls.AudioPlayer = null;
            return;
        }

        playbackControls.AudioPlayer = item.Player;
        playbackControls?.Show();

        // TODO: Temporary until there's a proper "album" art for audios
        slideTextureRect.Texture = item.MissingTexture;
    }

    private void UpdateHandles()
    {
        if (toolbar == null || closeButton == null)
            return;

        toolbar.Visible = slideControlsVisible;
        closeButton.Visible = slideControlsVisible;
    }

    private void OnScaledUp(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        SlideControlsVisible = true;
        FullRect = true;
        BoundToScreenArea = true;
    }

    private void OnScaledDown(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        SlideControlsVisible = true;
        Hide();
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
        SlideshowMode = pressed;
    }

    private void OnCloseButtonPressed()
    {
        CustomHide();
    }

    private void OnCloseButtonUpdate()
    {
        var icon = closeButton!.GetChild<TextureRect>(0);

        if (closeButton.GetDrawMode() == BaseButton.DrawMode.Pressed)
        {
            icon.Modulate = Colors.Black;
        }
        else
        {
            icon.Modulate = Colors.White;
        }
    }
}
