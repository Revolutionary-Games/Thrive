using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

public class SlideshowScreen : CustomDialog
{
    public const float SLIDESHOW_INTERVAL = 6f;
    public const float TOOLBAR_DISPLAY_DURATION = 4f;

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

    private TextureRect fullscreenRect = null!;
    private Control slideToolbar = null!;
    private Button slideNextButton = null!;
    private Button slidePrevButton = null!;
    private Button slideCloseButton = null!;
    private Button slideShowModeButton = null!;

    private Tween popupTween = null!;
    private Tween slideshowTween = null!;

    private float toolbarHideTimer;
    private float slideshowTimer;

    private int currentSlideIndex;
    private bool slideshowMode;

    public List<GalleryItem> SlideItems { get; set; } = null!;

    public int CurrentSlideIndex
    {
        get => currentSlideIndex;
        set
        {
            currentSlideIndex = value;
            UpdateSlideshow();
        }
    }

    public bool SlideshowMode
    {
        get => slideshowMode;
        set
        {
            slideshowMode = value;
            UpdateSlideshow();
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
        fullscreenRect = GetNode<TextureRect>(SlideTextureRectPath);
        slideToolbar = GetNode<Control>(SlideToolbarPath);
        slideNextButton = GetNode<Button>(SlideNextButtonPath);
        slidePrevButton = GetNode<Button>(SlidePrevButtonPath);
        slideCloseButton = GetNode<Button>(SlideCloseButtonPath);
        slideShowModeButton = GetNode<Button>(SlideShowModeButtonPath);

        popupTween = GetNode<Tween>("PopupTween");
        slideshowTween = GetNode<Tween>("SlideshowTween");

        UpdateSlideshow();
    }

    public override void _Process(float delta)
    {
        if (toolbarHideTimer >= 0 && Visible)
        {
            toolbarHideTimer -= delta;

            if (slideToolbar.Modulate.a < 1)
            {
                slideshowTween.InterpolateProperty(slideToolbar, "modulate:a", null, 1, 0.5f);
                slideshowTween.InterpolateProperty(slideCloseButton, "modulate:a", null, 1, 0.5f);
                slideshowTween.Start();
            }

            if (Input.GetMouseMode() == Input.MouseMode.Hidden)
                Input.SetMouseMode(Input.MouseMode.Visible);

            if (toolbarHideTimer < 0)
            {
                slideshowTween.InterpolateProperty(
                    slideToolbar, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
                slideshowTween.InterpolateProperty(
                    slideCloseButton, "modulate:a", null, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
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
        base.CustomShow();

        RectClipContent = true;

        slideToolbar.Visible = false;
        slideCloseButton.Visible = false;

        var currentItemRect = SlideItems[currentSlideIndex].GetGlobalRect();
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
        FullRect = false;
        RectClipContent = true;

        slideToolbar.Visible = false;
        slideCloseButton.Visible = false;

        var currentItemRect = SlideItems[currentSlideIndex].GetGlobalRect();

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
        currentSlideIndex = (currentSlideIndex + 1) % SlideItems.Count;

        ChangeSlide(fade);
    }

    public void RetreatSlide(bool fade = false)
    {
        --currentSlideIndex;

        if (currentSlideIndex < 0)
            currentSlideIndex = SlideItems.Count - 1;

        ChangeSlide(fade);
    }

    private void ChangeSlide(bool fade)
    {
        if (!Visible)
            CustomShow();

        if (!fade)
        {
            UpdateSlideshow();
            return;
        }

        slideshowTween.InterpolateProperty(fullscreenRect, "modulate", null, Colors.Black, 0.5f);
        slideshowTween.Start();

        if (!slideshowTween.IsConnected("tween_completed", this, nameof(OnSlideFaded)))
            slideshowTween.Connect("tween_completed", this, nameof(OnSlideFaded), null, (uint)ConnectFlags.Oneshot);
    }

    private void UpdateSlideshow()
    {
        if (fullscreenRect != null && SlideItems != null)
            fullscreenRect.Texture = GD.Load(SlideItems[currentSlideIndex].Asset.ResourcePath) as Texture;

        slideshowTimer = slideshowMode ? SLIDESHOW_INTERVAL : 0;
        slideShowModeButton?.SetPressedNoSignal(slideshowMode);
    }

    private void OnSlideFaded(Object @object, NodePath key)
    {
        UpdateSlideshow();
        slideshowTween.InterpolateProperty(fullscreenRect, "modulate", null, Colors.White, 0.5f);
        slideshowTween.Start();
    }

    private void OnScaledUp(Object @object, NodePath key)
    {
        slideToolbar.Visible = true;
        slideCloseButton.Visible = true;

        RectClipContent = false;
        FullRect = true;
    }

    private void OnScaledDown(Object @object, NodePath key)
    {
        slideToolbar.Visible = true;
        slideCloseButton.Visible = true;

        RectClipContent = false;
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
