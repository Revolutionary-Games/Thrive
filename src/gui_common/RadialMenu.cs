using System;
using System.Collections.Generic;
using Godot;

public partial class RadialMenu : CenterContainer
{
    [Export]
    public NodePath? CenterLabelPath;

    [Export]
    public NodePath DynamicLabelsContainerPath = null!;

    [Export]
    public NodePath IndicatorPath = null!;

#pragma warning disable CA2213
    [Export]
    public Texture2D HoveredItemHighlightBackground = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   For some reason I couldn't figure out the math to get the background to position perfectly without this
    /// </summary>
    [Export]
    public Vector2 HighlightBackgroundExtraOffset = new(0, -10);

    /// <summary>
    ///   Set to false when used in scenes, used for easily testing this through Godot editor
    /// </summary>
    [Export]
    public bool AutoShowTestData = true;

    [Export]
    public float MouseDeadzone = 50;

    [Export]
    public float LabelDistanceFromCenter = 250;

    /// <summary>
    ///   Used to center the text correctly
    /// </summary>
    [Export]
    public float MaxRadialLabelLength = 250;

    [Export]
    public float RadialCircleStart = 150;

    [Export]
    public float RadialCircleThickness = 200;

    [Export]
    public Color CircleColour = Colors.WhiteSmoke;

    [Export]
    public Color CircleHighlightColour = Colors.Aqua;

    private const int IndicatorSize = 32;

    private readonly List<LabelWithId> createdLabels = new();

#pragma warning disable CA2213
    private Label? centerLabel;
    private Node dynamicLabelsContainer = null!;
    private TextureRect indicator = null!;
#pragma warning restore CA2213

    private string centerText = Localization.Translate("SELECT_OPTION");

    // TODO: implement controller handling
    private Vector2? relativeMousePosition;

    [Signal]
    public delegate void OnItemSelectedEventHandler(int itemId);

    public string CenterText
    {
        get => centerText;
        set
        {
            centerText = value;
            UpdateCenterText();
        }
    }

    public int? HoveredItem { get; private set; }

    /// <summary>
    ///   We want first item to be at the top, so offset by one quarter rotation
    /// </summary>
    private double FirstItemAngle => -Math.PI * 0.5;

    public override void _Ready()
    {
        centerLabel = GetNode<Label>(CenterLabelPath);
        dynamicLabelsContainer = GetNode<Node>(DynamicLabelsContainerPath);
        indicator = GetNode<TextureRect>(IndicatorPath);

        UpdateCenterText();

        if (AutoShowTestData)
        {
            ShowWithItems(new[] { ("Item 1", 1), ("Item 2", 2), ("Third", 3), ("Item 4", 4), ("Last", 13) });
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        UpdateIndicator();

        MouseDefaultCursorShape = HoveredItem != null ? CursorShape.PointingHand : CursorShape.Arrow;

        // Let's hope there's so few labels that constantly updating their colours is not a problem
        foreach (var label in createdLabels)
        {
            label.AddThemeColorOverride("font_color", label.Id == HoveredItem ? CircleHighlightColour : Colors.White);
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        switch ((long)what)
        {
            case NotificationResized:
                if (Visible && centerLabel != null && createdLabels.Count > 0)
                    RepositionLabels();
                break;
        }
    }

    public override void _Draw()
    {
        base._Draw();

        var center = Size / 2;

        var circleEnd = RadialCircleStart + RadialCircleThickness;

        var smallCirclePoints = 60;
        var largeCirclePoints = 80;

        DrawArc(center, RadialCircleStart, 0, Mathf.Pi * 2, smallCirclePoints, CircleColour, 2, true);
        DrawArc(center, circleEnd, 0, Mathf.Pi * 2, largeCirclePoints, CircleColour, 2, true);

        // Draw lines between items
        var anglePerItem = CalculateAnglePerItem();
        var halfAngle = anglePerItem / 2;

        double currentAngle = FirstItemAngle;

        // The lines are drawn -half to +half around the angle for the items
        // Also we draw just one line per item to not draw lines over existing ones
        for (int i = 0; i < createdLabels.Count; ++i)
        {
            bool selected = HoveredItem == createdLabels[i].Id;

            bool previousSelected;

            if (i == 0)
            {
                previousSelected = HoveredItem == createdLabels[createdLabels.Count - 1].Id;
            }
            else
            {
                previousSelected = HoveredItem == createdLabels[i - 1].Id;
            }

            var direction = new Vector2((float)Math.Cos(currentAngle - halfAngle),
                (float)Math.Sin(currentAngle - halfAngle));

            // Draw a highlight background if selected and also some extra arc segments that are highlighted
            if (selected)
            {
                var directionFromCenterTowardsLabel =
                    new Vector2((float)Math.Cos(currentAngle), (float)Math.Sin(currentAngle));

                var imageCenterOffset = HoveredItemHighlightBackground.GetSize() / 2 + HighlightBackgroundExtraOffset;

                DrawTexture(HoveredItemHighlightBackground,
                    center + directionFromCenterTowardsLabel * LabelDistanceFromCenter - imageCenterOffset);

                DrawArc(center, RadialCircleStart, (float)(currentAngle - halfAngle), (float)(currentAngle + halfAngle),
                    smallCirclePoints / createdLabels.Count, CircleHighlightColour, 2, true);
                DrawArc(center, circleEnd, (float)(currentAngle - halfAngle), (float)(currentAngle + halfAngle),
                    largeCirclePoints / createdLabels.Count, CircleHighlightColour, 2, true);
            }

            DrawLine(center + direction * RadialCircleStart, center + direction * circleEnd,
                selected || previousSelected ? CircleHighlightColour : CircleColour, 2, true);

            currentAngle += anglePerItem;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Position is relative to us since we use _GuiInput
            relativeMousePosition = mouseMotion.Position;
        }
        else if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (AcceptHoveredItem())
                    GetViewport().SetInputAsHandled();
            }
        }
    }

    public void ShowWithItems(IEnumerable<(string Text, int Id)> items)
    {
        if (centerLabel == null)
            throw new SceneTreeAttachRequired();

        relativeMousePosition = null;
        HoveredItem = null;

        dynamicLabelsContainer.FreeChildren();
        createdLabels.Clear();

        foreach (var (text, id) in items)
        {
            var label = new LabelWithId(text, id);
            label.CustomMinimumSize = new Vector2(MaxRadialLabelLength, 0);
            dynamicLabelsContainer.AddChild(label);
            createdLabels.Add(label);
        }

        Visible = true;
        RepositionLabels();
    }

    [RunOnKeyDown("ui_accept", Priority = 2)]
    [RunOnKeyDown("e_primary", Priority = 2)]
    public bool AcceptHoveredItem()
    {
        if (!Visible || HoveredItem == null)
            return false;

        // There seems to be a Godot bug here where when this is hidden the cursor stays in the clickable state
        // until the cursor is moved. Seems like even overriding the cursor style here back to arrow doesn't work

        EmitSignal(SignalName.OnItemSelected, HoveredItem.Value);
        Visible = false;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CenterLabelPath != null)
            {
                CenterLabelPath.Dispose();
                DynamicLabelsContainerPath.Dispose();
                IndicatorPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateCenterText()
    {
        if (centerLabel == null)
            return;

        centerLabel.Text = centerText;
    }

    private void RepositionLabels()
    {
        var anglePerItem = CalculateAnglePerItem();

        double currentAngle = FirstItemAngle;

        // This is added to make the centers of the labels line up with the positions we calculate
        var centerOffset = new Vector2(-MaxRadialLabelLength / 2, 0);

        foreach (var label in createdLabels)
        {
            label.Position =
                new Vector2((float)Math.Cos(currentAngle), (float)Math.Sin(currentAngle)) *
                LabelDistanceFromCenter + centerOffset;
            currentAngle += anglePerItem;
        }

        QueueRedraw();
        UpdateIndicator();
    }

    private double CalculateAnglePerItem()
    {
        if (createdLabels.Count == 0)
            throw new DivideByZeroException("need to have more than 0 items");

        return Math.PI * 2 / createdLabels.Count;
    }

    private void UpdateIndicator()
    {
        if (relativeMousePosition == null)
        {
            indicator.Visible = false;
            return;
        }

        var center = Size / 2;
        var indicatorOffset = center - new Vector2(IndicatorSize / 2.0f, IndicatorSize);

        indicator.Visible = true;

        var mouseVectorFromCenter = relativeMousePosition.Value - center;
        var mouseVectorLength = mouseVectorFromCenter.Length();

        // Ignore mouse if too close to the center
        if (mouseVectorLength < MouseDeadzone)
        {
            indicator.Visible = false;

            if (HoveredItem != null)
            {
                HoveredItem = null;
                QueueRedraw();
            }

            return;
        }

        var mouseDirection = mouseVectorFromCenter / mouseVectorLength;
        var mouseAngle = mouseDirection.Angle();

        // In the indicator rotation coordinates the mouse is a quarter circle off
        indicator.Rotation = mouseAngle + Mathf.Pi * 0.5f;

        indicator.Position = new Vector2(Mathf.Cos(mouseAngle), Mathf.Sin(mouseAngle)) *
            RadialCircleStart + indicatorOffset;

        UpdateHoveredFromAngle(mouseAngle);
    }

    /// <summary>
    ///   Finds the item the user is hovering / selecting based on angle
    /// </summary>
    /// <param name="selectionAngle">The angle towards which the user is pointing</param>
    private void UpdateHoveredFromAngle(float selectionAngle)
    {
        float fullCircle = Mathf.Pi * 2;
        var anglePerItem = CalculateAnglePerItem();

        selectionAngle -= (float)FirstItemAngle;

        while (selectionAngle < 0)
            selectionAngle += fullCircle;

        while (selectionAngle > fullCircle)
            selectionAngle -= fullCircle;

        var itemIndex = (int)Math.Round(selectionAngle / anglePerItem);

        if (itemIndex == createdLabels.Count)
            itemIndex = 0;

        var previous = HoveredItem;
        HoveredItem = createdLabels[itemIndex].Id;
        if (previous != HoveredItem)
            QueueRedraw();
    }

    private partial class LabelWithId : Label
    {
        public LabelWithId(string text, int id)
        {
            Text = text;
            Id = id;

            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
        }

        public int Id { get; }
    }
}
