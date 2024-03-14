using Godot;

/// <summary>
///   A custom Control type which defines top-level Controls that also behaves like a Popup.
/// </summary>
/// <remarks>
///   <para>
///     Note that this doesn't have window decorations, for things that should act like windows use
///     <see cref="CustomWindow"/> as the base class / scene.
///   </para>
/// </remarks>
public partial class TopLevelContainer : Control
{
    private readonly Callable onRectSizeCallable;

    private bool mouseUnCaptureActive;
    private bool previousVisibilityState;

    private bool hasBeenRemovedFromTree;

    private bool readyCalled;

    public TopLevelContainer()
    {
        onRectSizeCallable = new Callable(this, nameof(ApplyRectSettings));
    }

    /// <summary>
    ///   Emitted when this window is closed or hidden.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Having a window closing animation results in the window's status be "closed" even though it's still visible
    ///     (for displaying the animation itself).
    ///   </para>
    /// </remarks>
    [Signal]
    public delegate void ClosedEventHandler();

    /// <summary>
    ///   Emitted when this is opened
    /// </summary>
    [Signal]
    public delegate void OpenedEventHandler();

    /// <summary>
    ///   Returns true if this window is closing (not yet hidden) after calling <see cref="Close"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This only becomes useful if you have a custom closing animation.
    ///   </para>
    /// </remarks>
    public bool Closing { get; private set; }

    /// <summary>
    ///   If true, clicking outside of this popup will not close it (only applies when this is acting as a popup).
    /// </summary>
    [Export]
    public bool Exclusive { get; set; }

    /// <summary>
    ///   If true and <see cref="Exclusive"/> is true, pressing ESC key will close the popup.
    /// </summary>
    [Export]
    public bool ExclusiveAllowCloseOnEscape { get; set; } = true;

    [Export]
    public bool PreventsMouseCaptureWhileOpen { get; set; } = true;

    /// <summary>
    ///   If true, the window size is locked to the size of the viewport.
    /// </summary>
    [Export]
    public bool FullRect { get; set; }

    protected bool MouseUnCaptureActive
    {
        set
        {
            if (!PreventsMouseCaptureWhileOpen)
            {
                if (mouseUnCaptureActive)
                {
                    SetMouseCaptureModeInternal(false);
                }

                return;
            }

            if (mouseUnCaptureActive == value)
                return;

            SetMouseCaptureModeInternal(value);
        }
    }

    public override void _Notification(int what)
    {
        // To reduce base method calls in derived types, we utilize notifications
        // TODO: refactoring this to work the normal way, would be pretty nice but requires all derived classes to be
        // checked to ensure they contain base calls in all of their overridden methods.

        switch ((long)what)
        {
            case NotificationEnterTree:
            {
                TopLevel = true;
                GetTree().Root.Connect(Viewport.SignalName.SizeChanged, onRectSizeCallable);

                // Special actions when re-entering the tree (and not when initially being added to the tree)
                if (hasBeenRemovedFromTree)
                {
                    // If this was re-parented (exited and re-entered the tree) while visible, we need to recheck the
                    // mouse capture state
                    if (IsVisibleInTree())
                        MouseUnCaptureActive = true;

                    hasBeenRemovedFromTree = false;
                }

                break;
            }

            case NotificationExitTree:
            {
                GetTree().Root.Disconnect(Viewport.SignalName.SizeChanged, onRectSizeCallable);

                MouseUnCaptureActive = false;
                hasBeenRemovedFromTree = true;
                break;
            }

            case NotificationReady:
                readyCalled = true;
                Hide();
                ApplyRectSettings();
                break;
        }

        // Workaround Godot 4 bug: https://github.com/godotengine/godot/issues/73908
        if (!readyCalled)
            return;

        switch ((long)what)
        {
            case NotificationResized:
                ApplyRectSettings();
                break;
            case NotificationVisibilityChanged:
            {
                if (previousVisibilityState == IsVisibleInTree())
                    break;

                previousVisibilityState = IsVisibleInTree();

                if (previousVisibilityState)
                {
                    MouseUnCaptureActive = true;
                    ApplyRectSettings();
                    OnOpen();
                    EmitSignal(SignalName.Opened);
                }
                else
                {
                    Closing = false;
                    MouseUnCaptureActive = false;
                    OnHidden();
                    EmitSignal(SignalName.Closed);
                }

                break;
            }
        }
    }

    /// <summary>
    ///   Shows this window with a custom behavior if implemented in <see cref="OnOpen"/>.
    /// </summary>
    public void Open()
    {
        if (Visible)
            return;

        // Showing the control is absolute, we shouldn't make it overridable by the user (as it won't affect
        // the opening animation, unlike `Close`).
        Show();

        // `OnOpen` will be called in _Notification.
    }

    /// <summary>
    ///   Opens this as a popup (modal window)
    /// </summary>
    public void OpenModal()
    {
        ModalManager.Instance.MakeModal(this);
        Notification((int)MissingGodotNotifications.NotificationPostPopup);
    }

    public void Open(bool modal, Rect2? rect = null)
    {
        if (modal)
        {
            OpenModal();
        }
        else
        {
            Open();
        }

        if (rect.HasValue)
        {
            Position = rect.Value.Position;
            Size = rect.Value.Size;
        }
    }

    /// <summary>
    ///   Shows this window at the center of the screen.
    /// </summary>
    public void OpenCentered(bool modal = true, Vector2? size = null)
    {
        var windowSize = GetViewportRect().Size;

        var rectPosition = ((windowSize - (size ?? Size) * Scale) / 2.0f).Floor();
        var rectSize = size ?? Size;

        Open(modal, new Rect2(rectPosition, rectSize));
    }

    /// <summary>
    ///   Shows this window by covering the whole screen.
    /// </summary>
    public void OpenFullRect()
    {
        Open(true, GetFullRect());
    }

    /// <summary>
    ///   Shows this control as a modal at the center of the screen and shrinks it to its minimum size.
    /// </summary>
    public void PopupCenteredShrink(bool runSizeUnstuck = true)
    {
        OpenCentered(true, GetMinimumSize());

        // In case the popup sizing stuck (this happens sometimes)
        if (runSizeUnstuck)
        {
            Invoke.Instance.Queue(() =>
            {
                this.MoveToCenter();

                // CustomRichTextLabel-based dialogs are especially vulnerable, thus do double unstucking
                Invoke.Instance.Queue(this.MoveToCenter);
            });
        }
    }

    /// <summary>
    ///   Hides this window with a custom behavior if implemented in <see cref="OnClose"/>.
    /// </summary>
    public void Close()
    {
        if (Closing || !Visible)
            return;

        Closing = true;
        OnClose();
    }

    /// <summary>
    ///   Called after this window is made visible. Implement custom open behavior by overriding this.
    /// </summary>
    protected virtual void OnOpen()
    {
        // Overridden methods can add a popping up animation
    }

    /// <summary>
    ///   Called when this window is closing from <see cref="Close"/>. Implement custom close behavior by
    ///   overriding this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The default implementation just hides. Unless you override this method, which then you must call
    ///     <see cref="OnClosingAnimationFinished"/> after your closing animation finishes.
    ///   </para>
    /// </remarks>
    protected virtual void OnClose()
    {
        // For an animation, override this method in a derived class and call `OnClosingAnimationFinished` once the
        // animation is complete (and don't call `base.OnClose` as that will hide things too early)
        OnClosingAnimationFinished();
    }

    /// <summary>
    ///   Called after this window is made invisible.
    /// </summary>
    protected virtual void OnHidden()
    {
    }

    protected virtual void OnClosingAnimationFinished()
    {
        Hide();
    }

    /// <summary>
    ///   Returns this window's rect in fullscreen mode.
    /// </summary>
    protected virtual Rect2 GetFullRect()
    {
        var viewportSize = GetViewportRect().Size;
        return new Rect2(Vector2.Zero, viewportSize);
    }

    /// <summary>
    ///   Applies final adjustments to this window's rect.
    /// </summary>
    protected virtual void ApplyRectSettings()
    {
        if (FullRect)
        {
            var fullRect = GetFullRect();
            Position = fullRect.Position;
            Size = fullRect.Size;
        }
    }

    /// <summary>
    ///   Applies the mouse capture mode. Do not call directly, use <see cref="MouseUnCaptureActive"/>
    /// </summary>
    private void SetMouseCaptureModeInternal(bool captured)
    {
        mouseUnCaptureActive = captured;

        // The name of this node is not allowed to change while visible, otherwise this will not work well
        var key = $"{GetType().Name}_{Name}";

        if (captured)
        {
            MouseCaptureManager.ReportOpenCapturePrevention(key);
        }
        else
        {
            MouseCaptureManager.ReportClosedCapturePrevention(key);
        }
    }
}
