using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

/// <summary>
///   Handles a stack of <see cref="CustomWindow"/>s that block GUI inputs.
/// </summary>
public class ModalManager : NodeWithInput
{
    private static ModalManager? instance;

    /// <summary>
    ///   Contains the original parents of modal windows. Used to return them to the right place in the scene after
    ///   they are no longer displayed.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<CustomWindow, Node> originalParents = new();

    private readonly Queue<CustomWindow> demotedModals = new();

    private Stack<CustomWindow> modalStack = new();

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CanvasLayer canvasLayer = null!;
    private Control activeModalContainer = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

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
    public bool IsTopMostPopupExclusive => GetTopMostPopupIfExclusive() != null;

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
    ///   Promotes the given popup as a modal and makes it visible (with a slight delay).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that if this is called quickly multiple times, only the modal that ends up being on top, will be
    ///     visible. Other modals will only become visible when they end up being the top-most one.
    ///   </para>
    ///   <para>
    ///     This does Node re-parenting operation, therefore calling this plainly in <see cref="Node._Ready"/> wouldn't
    ///     work as the Node is still busy then. Alternatively, you could defer the call in the next frame by using
    ///     <see cref="Invoke.Queue"/>.
    ///   </para>
    /// </remarks>
    public void MakeModal(CustomWindow popup)
    {
        if (modalStack.Contains(popup))
            return;

        originalParents[popup] = popup.GetParent();
        modalStack.Push(popup);
        modalsDirty = true;

        var binds = new Array { popup };
        popup.CheckAndConnect(nameof(CustomWindow.Closed), this, nameof(OnModalLost), binds,
            (uint)ConnectFlags.Oneshot);
    }

    /// <summary>
    ///   Closes the top-most modal popup if any.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideTopMostPopup()
    {
        if (modalStack.Count <= 0)
            return false;

        var popup = modalStack.Peek();

        if (popup.Exclusive && !popup.ExclusiveAllowCloseOnEscape)
            return false;

        // This is emitted before closing to allow window using components to differentiate between "cancel" and
        // "any other reason for closing" in case some logic can be simplified by handling just those two situations.
        if (popup is CustomDialog dialog)
            dialog.EmitSignal(nameof(CustomDialog.Cancelled));

        popup.Close();
        popup.Notification(Control.NotificationModalClose);

        return true;
    }

    /// <summary>
    ///   Returns the top-most popup in the modal stack if there's any and it's exclusive, otherwise null.
    /// </summary>
    public CustomWindow? GetTopMostPopupIfExclusive()
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
            return;
        }

        activeModalContainer.Show();

        var top = modalStack.Peek();

        foreach (var modal in modalStack)
        {
            if (modal != top)
            {
                modal.ReParent(canvasLayer);
                canvasLayer.MoveChild(modal, 0);
                continue;
            }

            top.ReParent(activeModalContainer);

            // Always make the top-most modal in the stack visible
            if (!modal.Visible)
                modal.Open();

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
                if (top is CustomDialog dialog)
                    dialog.EmitSignal(nameof(CustomDialog.Cancelled));

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

        if (modalStack.Peek() != popup)
        {
            // Unexpected modal reported being closed (not top most modal).
            // We kind of need to accept that this is inevitable as the order of when multiple windows are made modal
            // and closed is unpredictable, sometimes you could have two new modals but want to close the first, what
            // got closed instead is the second.

            // Removes the correct modal deeper in the stack
            modalStack = new Stack<CustomWindow>(modalStack.Where(m => m != popup));
        }
        else
        {
            modalStack.Pop();
        }

        demotedModals.Enqueue(popup);

        modalsDirty = true;
    }
}
