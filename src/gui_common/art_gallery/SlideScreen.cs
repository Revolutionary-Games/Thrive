using System.Collections.Generic;
using Godot;

/// <summary>
///   Screen capable of moving slides of gallery items.
/// </summary>
public partial class SlideScreen : TopLevelContainer
{
    public const float SLIDESHOW_INTERVAL = 6.0f;
    public const float TOOLBAR_DISPLAY_DURATION = 4.0f;

    [Export]
    public NodePath? SlideTextureRectPath;

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

    private readonly NodePath modulateAlphaReference = new("modulate:a");

#pragma warning disable CA2213
    private CrossFadableTextureRect? slideTextureRect;
    private Control? toolbar;
    private Button? closeButton;
    private Button? slideShowModeButton;
    private Label? slideTitleLabel;
    private CrossFadableGalleryViewport? modelViewerContainer;
    private SubViewport? modelViewer;
    private Node3D? modelHolder;
    private OrbitCamera? modelViewerCamera;
    private PlaybackControls? playbackControls;
#pragma warning restore CA2213

    private double toolbarHideTimer;
    private double slideshowTimer;

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

    public override void _Ready()
    {
        slideTextureRect = GetNode<CrossFadableTextureRect>(SlideTextureRectPath);
        toolbar = GetNode<Control>(SlideToolbarPath);
        closeButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);
        slideTitleLabel = GetNode<Label>(SlideTitleLabelPath);
        modelViewerContainer = GetNode<CrossFadableGalleryViewport>(ModelViewerContainerPath);
        modelViewer = GetNode<SubViewport>(ModelViewerPath);
        modelHolder = GetNode<Node3D>(ModelHolderPath);
        modelViewerCamera = GetNode<OrbitCamera>(ModelViewerCameraPath);
        playbackControls = GetNode<PlaybackControls>(PlaybackControlsPath);

        UpdateScreen();
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

    public override void _Process(double delta)
    {
        if (toolbarHideTimer >= 0 && Visible)
        {
            toolbarHideTimer -= delta;

            if (toolbar?.Modulate.A < 1)
            {
                var tween = CreateTween();
                tween.Parallel();

                tween.TweenProperty(toolbar, modulateAlphaReference, 1, 0.5);
                tween.TweenProperty(closeButton, modulateAlphaReference, 1, 0.5);
            }

            if (toolbarHideTimer < 0)
            {
                var tween = CreateTween();
                tween.Parallel();

                tween.TweenProperty(toolbar, modulateAlphaReference, 0, 0.5);
                tween.TweenProperty(closeButton, modulateAlphaReference, 0, 0.5);

                MouseCaptureManager.SetMouseHideState(true);
            }
            else
            {
                MouseCaptureManager.SetMouseHideState(false);
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

    protected override void OnOpen()
    {
        if (Items == null)
        {
            Hide();
            return;
        }

        SlideControlsVisible = false;

        var currentItemRect = Items[currentSlideIndex].GetGlobalRect();
        GlobalPosition = currentItemRect.Position;
        Size = currentItemRect.Size;

        var tween = CreateTween();
        tween.Parallel();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Sine);

        var fullRect = GetFullRect();
        tween.TweenProperty(this, "position", fullRect.Position, 0.2);
        tween.TweenProperty(this, "size", fullRect.Size, 0.2);

        tween.TweenCallback(new Callable(this, nameof(OnScaledUp)));
    }

    protected override void OnClose()
    {
        if (Items == null)
        {
            Hide();
            return;
        }

        FullRect = false;
        SlideControlsVisible = false;

        var currentItemRect = Items[currentSlideIndex].GetGlobalRect();

        var tween = CreateTween();
        tween.Parallel();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Sine);

        tween.TweenProperty(this, "position", currentItemRect.Position, 0.2);
        tween.TweenProperty(this, "size", currentItemRect.Size, 0.2);

        tween.TweenCallback(new Callable(this, nameof(OnScaledDown)));
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        SlideshowMode = false;
        MouseCaptureManager.SetMouseHideState(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SlideTextureRectPath != null)
            {
                SlideTextureRectPath.Dispose();
                SlideToolbarPath.Dispose();
                SlideCloseButtonPath.Dispose();
                SlideShowModeButtonPath.Dispose();
                SlideTitleLabelPath.Dispose();
                ModelViewerContainerPath.Dispose();
                ModelViewerPath.Dispose();
                ModelHolderPath.Dispose();
                ModelViewerCameraPath.Dispose();
                PlaybackControlsPath.Dispose();
            }

            modulateAlphaReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ChangeSlide(bool fade)
    {
        if (!Visible)
            Open();

        if (!fade)
        {
            UpdateScreen();
            return;
        }

        if (items == null || slideTextureRect == null)
            return;

        var item = items[currentSlideIndex];
        slideTextureRect.Image = GD.Load(item.Asset.ResourcePath) as Texture2D;

        if (slideTextureRect.Image != null)
            return;

        // If texture loading fails, the selected item is a model
        // These are handled here
        modelViewerContainer!.BeginFade();
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
        slideTextureRect.Texture = GD.Load(item.Asset.ResourcePath) as Texture2D;
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

        modelViewer.Msaa3D = Settings.Instance.MSAAResolution;

        var scene = GD.Load<PackedScene>(item.Asset.ResourcePath);
        var instanced = scene.Instantiate();

        modelHolder.AddChild(instanced);

        var mesh = instanced.GetNode<MeshInstance3D>(item.Asset.MeshNodePath!);
        var minDistance = (mesh.GlobalTransform * mesh.GetAabb()).Size.Length();
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

    private void OnScaledUp(GodotObject @object, NodePath key)
    {
        _ = @object;
        _ = key;

        SlideControlsVisible = true;
        FullRect = true;
    }

    private void OnScaledDown(GodotObject @object, NodePath key)
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
        Close();
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
