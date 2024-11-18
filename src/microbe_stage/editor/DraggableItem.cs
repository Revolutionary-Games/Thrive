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
    public delegate void OnDraggedToNewPositionEventHandler(DraggableItem item, DraggableItem newPositionBefore);

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
                Modulate = new Color(1, 1, 1, 0.7f);
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

    public static Control CreateDragPreview()
    {
        return new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Texture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/DragHandle.svg"),
            CustomMinimumSize = new Vector2(32, 32),
        };
    }

    public override void _Ready()
    {
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview(CreateDragPreview());

        // Mark as being dragged
        BeingDragged = true;

        return Variant.From(this);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Object)
            return false;

        var obj = data.AsGodotObject();

        if (obj is DraggableItem dragData)
        {
            // Don't allow drop into itself
            // TODO: should this react with a colour change to indicate valid drag?
            return dragData != this;
        }

        return false;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Object)
            return;

        var obj = data.AsGodotObject();

        if (obj is DraggableItem dragData)
        {
            dragData.BeingDragged = false;

            if (dragData != this)
                EmitSignal(SignalName.OnDraggedToNewPosition, this, dragData);
        }
    }

    public override void _Notification(int what)
    {
        // Reset drag status as this is the only way to detect when our created drag data object is no longer used
        if (what == NotificationDragEnd && beingDragged)
            BeingDragged = false;

        base._Notification(what);
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
