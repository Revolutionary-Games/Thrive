using System;
using Godot;

/// <summary>
///   Point / marker on a line chart containing a single data value.
/// </summary>
public class DataPoint : Control
{
    private Texture graphMarkerCircle;
    private Texture graphMarkerCross;

    private bool isMouseOver;

    private Vector2 coordinate;
    private float size;

    public DataPoint(float xValue, float yValue, float size = 8f, MarkerIcon shape = MarkerIcon.Circle,
        bool drawAtZeroValue = false)
    {
        Value = new Vector2(xValue, yValue);
        Size = size;
        IconType = shape;
        DrawAtZeroValue = drawAtZeroValue;
    }

    public enum MarkerIcon
    {
        Circle,
        Cross,
    }

    /// <summary>
    ///   Visual shape of the point
    /// </summary>
    public MarkerIcon IconType { get; set; }

    /// <summary>
    ///   Actual data the point represents
    /// </summary>
    public Vector2 Value { get; set; }

    /// <summary>
    ///   Position of the point on the chart, this is different from Value
    /// </summary>
    public Vector2 Coordinate
    {
        get => coordinate;
        set
        {
            coordinate = value;
            RectPosition = value - (RectSize / 2);
        }
    }

    /// <summary>
    ///   Visual size of the point
    /// </summary>
    public float Size
    {
        get => size;
        set
        {
            size = value;

            // Increased by 10 for a more bigger cursor detection area
            RectSize = new Vector2(value + 10, value + 10);
        }
    }

    public Color MarkerFillerColour { get; set; } = new Color(0.0f, 0.13f, 0.14f);

    public Color MarkerFillerHighlightedColour { get; set; } = new Color(0.07f, 1.0f, 0.84f);

    public Color MarkerColour { get; set; }

    /// <summary>
    ///   Used to hide marker visual while still keeping the tooltip for this data point hoverable
    /// </summary>
    public bool Draw { get; set; }

    /// <summary>
    ///   The Draw property is set to false if the point has zero value in the LineChart by default,
    ///   so this is used to flag the marker visual to keep being drawn.
    /// </summary>
    public bool DrawAtZeroValue { get; set; }

    public override void _Ready()
    {
        graphMarkerCircle = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCircle.png");
        graphMarkerCross = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCross.png");

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExit));

        Update();
    }

    public override void _Draw()
    {
        if (!Draw)
            return;

        var vectorSize = new Vector2(Size, Size);

        switch (IconType)
        {
            case MarkerIcon.Circle:
            {
                // Circle filler
                if (isMouseOver)
                {
                    DrawCircle(RectSize / 2, Size / 2, MarkerFillerHighlightedColour);
                }
                else
                {
                    DrawCircle(RectSize / 2, Size / 2, MarkerFillerColour);
                }

                DrawTextureRect(graphMarkerCircle, new Rect2(
                    (RectSize / 2) - (vectorSize / 2), vectorSize), false, MarkerColour);

                break;
            }

            case MarkerIcon.Cross:
                DrawTextureRect(graphMarkerCross, new Rect2(
                    (RectSize / 2) - (vectorSize / 2), vectorSize), false, MarkerColour);
                break;
            default:
                throw new Exception("Invalid marker shape");
        }
    }

    private void OnMouseEnter()
    {
        isMouseOver = true;

        Update();
    }

    private void OnMouseExit()
    {
        isMouseOver = false;

        Update();
    }
}
