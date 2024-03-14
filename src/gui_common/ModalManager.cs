using System.Collections.Generic;
using System.Linq;
using Godot;
using Nito.Collections;

/// <summary>
///   Handles a stack of <see cref="TopLevelContainer"/>s that block GUI inputs.
/// </summary>
public partial class ModalManager : NodeWithInput
{
    private static ModalManager? instance;

    /// <summary>
    ///   Contains the original parents of modal windows. Used to return them to the right place in the scene after
    ///   they are no longer displayed.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<TopLevelContainer, Node> originalParents = new();

    private readonly Deque<TopLevelContainer> modalStack = new();
    private readonly Queue<TopLevelContainer> demotedModals = new();

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CanvasLayer canvasLayer = null!;
    private Control activeModalContainer = null!;

    private TopLevelContainer? topMostWindowGivenFocus;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private bool modalsDirty = true;

    private ModalManager()
    {
        instance = this;

        ProcessMode = ProcessModeEnum.Always;
    }

    public static ModalManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Checks whether the top-most modal on the modal stack is an exclusive popup.
    /// </summary>
    public bool IsTopMostPopupExclusive => GetTopMostPopupIfExclusive() != null;

    public override void _Ready()
    {
        canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
        activeModalContainer = canvasLayer.GetNode<Control>("ActiveModalContainer");

        activeModalContainer.TopLevel = true;
    }

    public override void _Process(double delta)
    {
        if (modalsDirty)
        {
            UpdateModals();
            modalsDirty = false;
        }
    }

    /// <summary>
    ///   Promotes the given popup as a modal and makes it visible (with a slight delay).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This does Node re-parenting operation, therefore calling this plainly in <see cref="Node._Ready"/> wouldn't
    ///     work as the Node is still busy then. Alternatively, you could defer the call in the next frame by using
    ///     <see cref="Invoke.Queue"/>.
    ///   </para>
    /// </remarks>
    public void MakeModal(TopLevelContainer popup)
    {
        if (modalStack.Contains(popup))
            return;

        originalParents[popup] = popup.GetParent();
        modalStack.AddToFront(popup);
        modalsDirty = true;

        popup.CheckAndConnect(TopLevelContainer.SignalName.Closed, Callable.From(() => OnModalLost(popup)),
            (uint)ConnectFlags.OneShot);
    }

    /// <summary>
    ///   Closes the top-most modal popup if any.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideTopMostPopup()
    {
        if (modalStack.Count <= 0)
            return false;

        var popup = modalStack.First();

        if (popup.Exclusive && !popup.ExclusiveAllowCloseOnEscape)
            return false;

        // This is emitted before closing to allow window using components to differentiate between "cancel" and
        // "any other reason for closing" in case some logic can be simplified by handling just those two situations.
        if (popup is CustomWindow dialog)
            dialog.EmitSignal(CustomWindow.SignalName.Canceled);

        popup.Close();

        // TODO: make sure removing this is not a problem (looks like this signal no longer exists at all)
        // popup.Notification(Control.NotificationModalClose);

        return true;
    }

    /// <summary>
    ///   Returns the top-most popup in the modal stack if there's any and it's exclusive, otherwise null.
    /// </summary>
    public TopLevelContainer? GetTopMostPopupIfExclusive()
    {
        if (modalStack.Count <= 0)
            return null;

        var modal = modalStack.First();

        if (modal.Exclusive)
            return modal;

        return null;
    }

    private void UpdateModals()
    {
        while (demotedModals.Count > 0)
        {
            var modal = demotedModals.Dequeue();

            if (!originalParents.TryGetValue(modal, out Node parent))
            {
                modal.ReParent(this);
                continue;
            }

            // TODO: Consider returning the modal to its original position in its original parent?
            modal.ReParent(parent);
        }

        if (modalStack.Count <= 0)
        {
            activeModalContainer.Hide();
            topMostWindowGivenFocus = null;
            return;
        }

        activeModalContainer.Show();

        var top = modalStack.First();

        foreach (var modal in modalStack)
        {
            if (modal != top)
            {
                modal.ReParent(canvasLayer);
                canvasLayer.MoveChild(modal, 0);
            }
            else
            {
                top.ReParent(activeModalContainer);
            }

            // The user expects all modal in the stack to be visible (see `MakeModal` documentation).
            // For unexplained reasons this has to be after the re-parenting operation for focus to work in the popups.
            // So don't move this code anywhere else without a ton of testing verifying things still work.
            if (!modal.Visible)
            {
                modal.Open();
                modal.Notification((int)MissingGodotNotifications.NotificationPostPopup);
            }
        }

        // Always give focus to the top-most modal in the stack (unless we already did so to avoid overriding focus
        // after the user has had a chance to change the focus in the modal)
        if (topMostWindowGivenFocus != top)
        {
            // We use our custom method here to prefer to not give focus to nodes that are in a disabled state
            top.FirstFocusableControl()?.GrabFocus();
            topMostWindowGivenFocus = top;
        }
    }

    private void OnModalContainerInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            // User has pressed somewhere outside of the popup's area

            // Don't count mouse wheel, this is the original Godot behavior
            if (modalStack.Count <= 0 || mouseButton.ButtonIndex is
                    MouseButton.WheelDown or MouseButton.WheelUp or
                    MouseButton.WheelLeft or MouseButton.WheelRight)
            {
                return;
            }

            var top = modalStack.First();

            if (!top.Exclusive)
            {
                if (top is CustomWindow dialog)
                    dialog.EmitSignal(CustomWindow.SignalName.Canceled);

                // The crux of the custom modal system, to have an overridable hide behavior!
                top.Close();

                // TODO: make sure removing this is not a problem (looks like this signal no longer exists at all)
                // top.Notification(Control.NotificationModalClose);
            }
        }
    }

    /// <summary>
    ///   Called when <paramref name="popup"/> is closed.
    /// </summary>
    private void OnModalLost(TopLevelContainer popup)
    {
        if (!modalStack.Contains(popup))
            return;

        if (modalStack.First() != popup)
        {
            // Unexpected modal reported being closed (not top most modal).
            // We kind of need to accept that this is inevitable as the order of when multiple windows are made modal
            // and closed is unpredictable, sometimes you could have two new modals but want to close the first, what
            // got closed instead is the second.

            // Removes the correct modal deeper in the stack
            modalStack.Remove(popup);
        }
        else
        {
            modalStack.RemoveFromFront();
        }

        demotedModals.Enqueue(popup);

        modalsDirty = true;
    }
}
