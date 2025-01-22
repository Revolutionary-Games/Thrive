using System.Collections.Generic;
using System.Linq;
using Godot;
using Nito.Collections;

/// <summary>
///   Handles a stack of <see cref="TopLevelContainer"/>s that block GUI inputs.
/// </summary>
[GodotAutoload]
public partial class ModalManager : NodeWithInput
{
    private static ModalManager? instance;

    /// <summary>
    ///   Contains the original parents of modal windows. Used to return them to the right place in the scene after
    ///   they are no longer displayed.
    /// </summary>
    private readonly Dictionary<TopLevelContainer, Node> originalParents = new();

    /// <summary>
    ///   Contains the callable that is used to delete the modal when the parent leaves the tree.
    /// </summary>
    private readonly Dictionary<TopLevelContainer, Callable> parentLostCallables = new();

    private readonly Deque<TopLevelContainer> modalStack = new();
    private readonly Queue<TopLevelContainer> demotedModals = new();

    private readonly StringName modalConnectedMeta = "ModalManagerConnected";

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

    public override void _ExitTree()
    {
        base._ExitTree();

        if (instance == this)
            instance = null;
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

        var parent = popup.GetParent();
        originalParents[popup] = parent;

        // Listen for when the parent is removed from the tree so the modal can be removed as well.
        // But only if not registered already to avoid a duplicate connection
        if (!parentLostCallables.ContainsKey(popup))
        {
            var parentLostCallable = Callable.From(() => OnParentLost(popup));
            parent.Connect(Node.SignalName.TreeExiting, parentLostCallable, (uint)ConnectFlags.OneShot);
            parentLostCallables[popup] = parentLostCallable;
        }
        else
        {
            GD.PrintErr("Modal is becoming modal again without being closed, not registering exit signal");
        }

        modalStack.AddToFront(popup);
        modalsDirty = true;

        // Connect closed signal only when not already done so
        if (popup.HasMeta(modalConnectedMeta))
            return;

        popup.SetMeta(modalConnectedMeta, Variant.From(true));

        // PROBLEM:
        popup.Connect(TopLevelContainer.SignalName.Closed, Callable.From(() => OnModalLost(popup)),
            (uint)ConnectFlags.OneShot);
    }

    /// <summary>
    ///   Closes the top-most modal popup, if any.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideTopMostPopup()
    {
        if (modalStack.Count <= 0)
            return false;

        var popup = modalStack.First();

        if (popup.Exclusive && !popup.ExclusiveAllowCloseOnEscape)
            return false;

        ClosePopupDialog(popup);

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

    /// <summary>
    ///   Tries to cancel all open modals (or close if the modals don't support cancellation)
    /// </summary>
    /// <returns>True if all modals are closed, false if some refused to close</returns>
    public bool TryCancelModals()
    {
        // If no modals, we don't need to do anything
        if (modalStack.Count <= 0)
            return true;

        while (modalStack.Count > 0)
        {
            var modal = modalStack.First();

            // TODO: implement a way for important popups to ignore this close
            ClosePopupDialog(modal);

            // Fail if modal is still not closed
            if (modal.IsVisibleInTree())
            {
                GD.Print("Modal doesn't want to cancel / close failing close operation of all modals");
                return false;
            }
        }

        // All modals are now closed
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            modalConnectedMeta.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ClosePopupDialog(TopLevelContainer popup)
    {
        // This is emitted before closing to allow window-using components to differentiate between "cancel" and
        // "any other reason for closing" in case some logic can be simplified by handling just those two situations.
        if (popup is CustomWindow dialog)
            dialog.EmitSignal(CustomWindow.SignalName.Canceled);

        popup.Close();

        // TODO: make sure removing this is not a problem (looks like this signal no longer exists at all)
        // popup.Notification(Control.NotificationModalClose);
    }

    /// <summary>
    ///   Parent lost signals are added when a modal is created so that the modal can be automatically deleted.
    /// </summary>
    private void DeleteParentLostSignal(TopLevelContainer modal)
    {
        if (!originalParents.TryGetValue(modal, out var parent))
            return;
        if (!parentLostCallables.TryGetValue(modal, out var parentLostCallable))
            return;

        parent.Disconnect(Node.SignalName.TreeExiting, parentLostCallable);
        parentLostCallables.Remove(modal);
    }

    private void UpdateModals()
    {
        while (demotedModals.Count > 0)
        {
            var modal = demotedModals.Dequeue();

            DeleteParentLostSignal(modal);

            if (!originalParents.TryGetValue(modal, out var parent))
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
            // User has pressed somewhere outside the popup's area

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
        // Remove meta signal connection info as this should only be triggered through the signal
        popup.RemoveMeta(modalConnectedMeta);

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

    /// <summary>
    ///   Called when the parent of a <paramref name="popup"/> is deleted from the scene tree.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If a popup is demoted but its parent is invalid, an exception will occur.
    ///     Deleting the modal here and removing references to it will avoid the exception.
    ///   </para>
    /// </remarks>
    private void OnParentLost(TopLevelContainer popup)
    {
        // Remove the original parent since the reference will now be invalid
        var popupWasInDictionary = originalParents.Remove(popup);

        // Guard against duplicate signals
        if (!popupWasInDictionary)
            return;

        // Delete the popup
        popup.Close();
        modalStack.Remove(popup);
        popup.QueueFree();
        DeleteParentLostSignal(popup);

        // Remove the popup from the demoted modals since it is now destroyed
        var demotedModalsCount = demotedModals.Count;
        for (int i = 0; i < demotedModalsCount; ++i)
        {
            var item = demotedModals.Dequeue();
            if (item != popup)
                demotedModals.Enqueue(item);
        }

        modalsDirty = true;
    }
}
