using System.Collections.Generic;
using Godot;
using Godot.Collections;

/// <summary>
///   Handles a stack of <see cref="CustomWindow"/>s that blocks GUI inputs.
/// </summary>
public class ModalManager : NodeWithInput
{
    private static ModalManager? instance;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CanvasLayer canvasLayer = null!;
    private Control activeModalContainer = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private System.Collections.Generic.Dictionary<CustomWindow, Node> parents = new();
    private Stack<CustomWindow> modalStack = new();

    private bool modalsDirty = true;

    private ModalManager()
    {
        instance = this;

        PauseMode = PauseModeEnum.Process;
    }

    public static ModalManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Checks whether the top-most modal on the modal stack is an exclusive popup.
    /// </summary>
    public bool IsAnyExclusivePopupActive => GetCurrentlyActiveExclusivePopup() != null;

    public override void _Ready()
    {
        canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
        activeModalContainer = canvasLayer.GetNode<Control>("ActiveModalContainer");

        activeModalContainer.SetAsToplevel(true);
    }

    public override void _Process(float delta)
    {
        if (modalsDirty)
        {
            UpdateModals();
            modalsDirty = false;
        }
    }

    /// <summary>
    ///   Promotes the given popup as a modal and make it visible.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This does Node reparenting operation, therefore calling this plainly in <see cref="Node._Ready"/> wouldn't
    ///     work as the Node is still busy then. Alternatively, you could defer the call in the next frame by using
    ///     <see cref="Invoke.Queue"/>.
    ///   </para>
    /// </remarks>
    public void MakeModal(CustomWindow popup)
    {
        if (modalStack.Contains(popup))
            return;

        parents[popup] = popup.GetParent();
        modalStack.Push(popup);
        modalsDirty = true;

        var bind = new Array { popup };
        popup.CheckAndConnect("hide", this, nameof(OnModalLost), bind, (uint)ConnectFlags.Oneshot);

        if (!popup.Visible)
            popup.Open();
    }

    /// <summary>
    ///   Closes any currently active modal popups.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideCurrentlyActivePopup()
    {
        if (modalStack.Count <= 0)
            return false;

        var popup = modalStack.Peek();

        if (popup.Exclusive && !popup.ExclusiveAllowCloseOnEscape)
            return false;

        popup.Close();
        popup.Notification(Control.NotificationModalClose);

        if (popup is CustomDialog dialog)
            dialog.EmitSignal(nameof(CustomDialog.Canceled));

        return true;
    }

    /// <summary>
    ///   Returns the top-most exclusive popup in the current Viewport's modal stack. Null if there is none.
    /// </summary>
    public CustomWindow? GetCurrentlyActiveExclusivePopup()
    {
        if (modalStack.Count <= 0)
            return null;

        var modal = modalStack.Peek();

        if (modal.Exclusive)
            return modal;

        return null;
    }

    private void UpdateModals()
    {
        activeModalContainer.Hide();

        if (modalStack.Count <= 0)
            return;

        var top = modalStack.Peek();

        foreach (var modal in modalStack)
        {
            if (modal != top)
            {
                modal.ReParent(canvasLayer);
                canvasLayer.MoveChild(modal, 0);
                continue;
            }

            activeModalContainer.Show();

            top.ReParent(activeModalContainer);

            // Always give focus to the top-most modal in the stack
            top.FindNextValidFocus()?.GrabFocus();
        }
    }

    private void OnModalContainerInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            // User has pressed somewhere outside of the popup's area

            // Don't count mouse wheel, this is the original Godot behavior
            if (modalStack.Count <= 0 || mouseButton.ButtonIndex is
                    (int)ButtonList.WheelDown or (int)ButtonList.WheelUp or
                    (int)ButtonList.WheelLeft or (int)ButtonList.WheelRight)
            {
                return;
            }

            var top = modalStack.Peek();

            if (!top.Exclusive)
            {
                // The crux of the custom modal system, to have an overridable hide behavior!
                top.Close();
                top.Notification(Control.NotificationModalClose);
            }
        }
    }

    /// <summary>
    ///   Called when <paramref name="popup"/> is closed.
    /// </summary>
    private void OnModalLost(CustomWindow popup)
    {
        if (!modalStack.Contains(popup))
            return;

        if (!parents.TryGetValue(popup, out Node parent))
        {
            popup.ReParent(this);
            return;
        }

        var modal = modalStack.Pop();
        modal.ReParent(parent);

        modalsDirty = true;
    }
}
