using Godot;

/// <summary>
///   A custom Control type which defines top-level Controls that also behaves like a Popup.
/// </summary>
public class CustomWindow : Control
{
    private bool mouseUnCaptureActive;

    /// <summary>
    ///   If true, clicking outside of this popup will not close it.
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

    private bool MouseUnCaptureActive
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

        switch (what)
        {
            case NotificationEnterTree:
                SetAsToplevel(true);
                GetTree().Root.Connect("size_changed", this, nameof(ApplyRectSettings));
                break;
            case NotificationExitTree:
                MouseUnCaptureActive = false;
                GetTree().Root.Disconnect("size_changed", this, nameof(ApplyRectSettings));
                break;
            case NotificationReady:
                Hide();
                ApplyRectSettings();
                break;
            case NotificationResized:
                ApplyRectSettings();
                break;
            case NotificationVisibilityChanged:
                if (IsVisibleInTree())
                {
                    MouseUnCaptureActive = true;
                    ApplyRectSettings();
                    OnShown();
                }
                else
                {
                    MouseUnCaptureActive = false;
                    OnHidden();
                }

                break;
        }
    }

    /// <summary>
    ///   Shows this popup with a custom behavior, if any.
    /// </summary>
    public virtual void Open()
    {
        Show();
    }

    public void OpenModal()
    {
        ModalManager.Instance.MakeModal(this);
        Notification(Popup.NotificationPostPopup);
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
            RectPosition = rect.Value.Position;
            RectSize = rect.Value.Size;
        }
    }

    /// <summary>
    ///   Shows this popup at the center of the screen.
    /// </summary>
    public void OpenCentered(bool modal = true, Vector2? size = null)
    {
        var windowSize = GetViewportRect().Size;

        var rectPosition = ((windowSize - (size ?? RectSize) * RectScale) / 2.0f).Floor();
        var rectSize = size ?? RectSize;

        Open(modal, new Rect2(rectPosition, rectSize));
    }

    /// <summary>
    ///   Shows this popup by covering the whole screen.
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
                Invoke.Instance.Queue(() => this.MoveToCenter());
            });
        }
    }

    /// <summary>
    ///   Hides this popup with a custom behavior, if any.
    /// </summary>
    public virtual void Close()
    {
        Hide();
    }

    /// <summary>
    ///   Called after popup is made visible.
    /// </summary>
    protected virtual void OnShown()
    {
    }

    /// <summary>
    ///   Called after popup is made invisible.
    /// </summary>
    protected virtual void OnHidden()
    {
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
            RectPosition = fullRect.Position;
            RectSize = fullRect.Size;
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
