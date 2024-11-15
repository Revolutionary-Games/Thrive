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
    [Export]
    private Label position = null!;

    [Export]
    private Label nameLabel = null!;

    [Export]
    private Button moveUpButton = null!;

    [Export]
    private Button moveDownButton = null!;
#pragma warning restore CA2213

    private int numericPosition = -1;

    private bool canMoveUp;
    private bool canMoveDown = true;
    private bool beingDragged;

    [Signal]
    public delegate void OnUpPressedEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDownPressedEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDragStartEventHandler(DraggableItem item);

    [Signal]
    public delegate void OnDragEndEventHandler(DraggableItem item);

    [Export]
    public bool CanMoveUp
    {
        get => canMoveUp;
        set
        {
            canMoveUp = value;
            moveUpButton.Disabled = BeingDragged || !canMoveUp;
        }
    }

    [Export]
    public bool CanMoveDown
    {
        get => canMoveDown;
        set
        {
            canMoveDown = value;
            moveDownButton.Disabled = BeingDragged || !canMoveDown;
        }
    }

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
            position.Text = Localization.Translate("POSITION_NUMBER").FormatSafe(numericPosition);
        }
    }

    /// <summary>
    ///   True when this is being dragged by the user
    /// </summary>
    public bool BeingDragged
    {
        get => beingDragged;
        private set
        {
            beingDragged = value;

            // Update dependent button statuses
            CanMoveUp = canMoveUp;
            CanMoveDown = canMoveDown;

            if (beingDragged)
            {
                Modulate = new Color(1, 1, 1, 0.8f);
            }
            else
            {
                Modulate = new Color(1, 1, 1, 1.0f);
            }
        }
    }

    /// <summary>
    ///   Freely usable data by outside classes to store here
    /// </summary>
    public object? UserData { get; set; }

    public override void _Ready()
    {
    }

    public void SetLabelText(string readableText)
    {
        nameLabel.Text = readableText;
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
