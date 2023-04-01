using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Handles a stack of <see cref="CustomPopup"/>s that blocks GUI inputs.
/// </summary>
public class ModalManager : NodeWithInput
{
    private static ModalManager? instance;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CanvasLayer canvasLayer = null!;
    private Control activeModalContainer = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private Dictionary<CustomPopup, Node> parents = new();
    private Stack<CustomPopup> modalStack = new();

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
    public void MakeModal(CustomPopup popup)
    {
        if (modalStack.Contains(popup))
            return;

        parents[popup] = popup.GetParent();
        modalStack.Push(popup);
        modalsDirty = true;

        var bind = new Array { popup };
        popup.CheckAndConnect("hide", this, nameof(OnModalLost), bind, (uint)ConnectFlags.Oneshot);
        popup.CheckAndConnect("focus_exited", this, nameof(OnModalLost), bind, (uint)ConnectFlags.Oneshot);

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

        if (popup is CustomDialog dialog)
            dialog.EmitSignal(nameof(CustomDialog.Dismissed));

        return true;
    }

    /// <summary>
    ///   Returns the top-most exclusive popup in the current Viewport's modal stack. Null if there is none.
    /// </summary>
    public CustomPopup? GetCurrentlyActiveExclusivePopup()
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
        }
    }

    private void OnModalContainerInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            // User has pressed outside of the popup's area

            // Not counting mouse wheel which is the original default behavior
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
            }
        }
    }

    private void OnModalLost(CustomPopup popup)
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
        modal.Close();

        modalsDirty = true;
    }
}
