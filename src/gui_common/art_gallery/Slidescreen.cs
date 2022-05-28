using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

/// <summary>
///   Screen capable of moving slides of gallery items.
/// </summary>
public class Slidescreen : CustomDialog
{
    public const float SLIDESHOW_INTERVAL = 6f;
    public const float TOOLBAR_DISPLAY_DURATION = 4f;

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
    public NodePath AudioPlayerSliderPath = null!;

    [Export]
    public NodePath PlayButtonPath = null!;

    private TextureRect? fullscreenTextureRect;
    private Control? toolbar;
    private Button? closeButton;
    private Button? slideShowModeButton;
    private Label? slideTitleLabel;
    private ViewportContainer? modelViewerContainer;
    private Viewport? modelViewer;
    private Spatial? modelHolder;
    private OrbitCamera? modelViewerCamera;

    private Tween popupTween = null!;
    private Tween slideshowTween = null!;

    private float toolbarHideTimer;
    private float slideshowTimer;

    private int currentSlideIndex;
    private bool slideshowMode;
    private bool handlesVisible;

    public List<GalleryCard>? Items { get; set; }

    public int CurrentSlideIndex
    {
        get => currentSlideIndex;
        set
        {
            currentSlideIndex = value;
            UpdateSlide();
            UpdateModelViewer();
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
    ///   Handles means the UI elements on the slideshow screen (e.g next and prev slide button). If false they'll
    ///   be hidden.
    /// </summary>
    public bool HandlesVisible
    {
        get => handlesVisible;
        set
        {
            handlesVisible = value;
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
        fullscreenTextureRect = GetNode<TextureRect>(SlideTextureRectPath);
        toolbar = GetNode<Control>(SlideToolbarPath);
        closeButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);
        slideTitleLabel = GetNode<Label>(SlideTitleLabelPath);
        modelViewerContainer = GetNode<ViewportContainer>(ModelViewerContainerPath);
        modelViewer = GetNode<Viewport>(ModelViewerPath);
        modelHolder = GetNode<Spatial>(ModelHolderPath);
        modelViewerCamera = GetNode<OrbitCamera>(ModelViewerCameraPath);

        popupTween = GetNode<Tween>("PopupTween");
        slideshowTween = GetNode<Tween>("SlideshowTween");

        UpdateSlide();
        UpdateModelViewer();
        UpdateHandles();
    }

    public override void _Process(float delta)
    {
        if (toolbarHideTimer >= 0 && Visible)
        {
            toolbarHideTimer -= delta;

            if (toolbar?.Modulate.a < 1)
            {
                slideshowTween.InterpolateProperty(toolbar, "modulate:a", null, 1, 0.5f);
                slideshowTween.InterpolateProperty(closeButton, "modulate:a", null, 1, 0.5f);
                slideshowTween.Start();
            }

            if (Input.GetMouseMode() == Input.MouseMode.Hidden)
                Input.SetMouseMode(Input.MouseMode.Visible);

            if (toolbarHideTimer < 0)
            {
                slideshowTween.InterpolateProperty(
                    toolbar, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                slideshowTween.InterpolateProperty(
                    closeButton, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                slideshowTween.Start();
                Input.SetMouseMode(Input.MouseMode.Hidden);
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
    public void OnSlideToRight()
    {
        AdvanceSlide();
    }

    [RunOnKeyDownWithRepeat("ui_left", OnlyUnhandled = false)]
    public void OnSlideToLeft()
    {
        RetreatSlide();
    }

    public override void CustomShow()
    {
        if (Items == null)
            return;

        base.CustomShow();

        HandlesVisible = false;

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
        HandlesVisible = false;

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

    public void AdvanceSlide(bool fade = false)
    {
        if (Items == null)
            return;

        currentSlideIndex = (currentSlideIndex + 1) % Items.Count;

        ChangeSlide(fade);
    }

    public void RetreatSlide(bool fade = false)
    {
        if (Items == null)
            return;

        --currentSlideIndex;

        if (currentSlideIndex < 0)
            currentSlideIndex = Items.Count - 1;

        ChangeSlide(fade);
    }

    private void ChangeSlide(bool fade)
    {
        if (!Visible)
            CustomShow();

        if (!fade)
        {
            UpdateSlide();
            UpdateModelViewer();
            return;
        }

        slideshowTween.InterpolateProperty(fullscreenTextureRect, "modulate", null, Colors.Black, 0.5f);
        slideshowTween.Start();

        if (!slideshowTween.IsConnected("tween_completed", this, nameof(OnSlideFaded)))
            slideshowTween.Connect("tween_completed", this, nameof(OnSlideFaded), null, (uint)ConnectFlags.Oneshot);
    }

    private void UpdateSlide()
    {
        if (Items == null || slideTitleLabel == null || fullscreenTextureRect == null)
            return;

        var item = Items[currentSlideIndex];

        slideshowTimer = slideshowMode ? SLIDESHOW_INTERVAL : 0;
        slideShowModeButton?.SetPressedNoSignal(slideshowMode);

        slideTitleLabel.Text = item.Asset.Title;
        fullscreenTextureRect.Texture = GD.Load(item.Asset.ResourcePath) as Texture;
    }

    private void UpdateModelViewer()
    {
        if (Items == null)
            return;

        var item = Items[currentSlideIndex];

        if (item.Asset.Type != AssetType.ModelScene || modelViewerContainer == null || modelHolder == null ||
            modelViewer == null || modelViewerCamera == null)
        {
            modelViewerContainer?.Hide();
            return;
        }

        modelViewerContainer.Show();
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

    private void UpdateHandles()
    {
        if (toolbar == null || closeButton == null)
            return;

        toolbar.Visible = handlesVisible;
        closeButton.Visible = handlesVisible;
    }

    private void OnSlideFaded(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        UpdateSlide();
        UpdateModelViewer();
        slideshowTween.InterpolateProperty(fullscreenTextureRect, "modulate", null, Colors.White, 0.5f);
        slideshowTween.Start();
    }

    private void OnScaledUp(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        HandlesVisible = true;
        FullRect = true;
    }

    private void OnScaledDown(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        HandlesVisible = true;
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

    private void OnHidden()
    {
        SlideshowMode = false;
        Input.SetMouseMode(Input.MouseMode.Visible);
    }
}
