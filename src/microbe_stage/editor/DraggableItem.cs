using Godot;

/// <summary>
///   Items in for example <see cref="GrowthOrderPicker"/> that can be dragged around by the player in the GUI
/// </summary>
/// <remarks>
///   <para>
///     TODO: needs to have support specifically added for controller manipulation of these items
///   </para>
/// </remarks>
public partial class DraggableItem : Control
{
#pragma warning disable CA2213
    private Label position = null!;
#pragma warning restore CA2213

    private int numericPosition = -1;

    [Signal]
    public delegate void OnUpPressedEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDownPressedEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDragStartEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDragEndEventHandler(DraggableItem item);

    /// <summary>
    ///   Position number shown in this. Doesn't automatically reorder anything.
    /// </summary>
    public int PositionNumber
    {
        get => numericPosition;
        set
        {
            if (numericPosition == value)
                return;

            numericPosition = value;
            position.Text = numericPosition.ToString();
        }
    }

    // TODO: dragged property to make this visually distinct

    public override void _Ready()
    {
    }

    private void OnUpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnUpPressed, this);
    }

    private void OnDownButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnDownPressed, this);
    }
}
