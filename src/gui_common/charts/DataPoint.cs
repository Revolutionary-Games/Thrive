using System;
using Godot;

/// <summary>
///   Point / marker on a chart containing a single numerical data value (x, y).
///   This inherits Control to make this interactable as well as for giving it a visual marker.
/// </summary>
/// <remarks>
///   <para>
///     Note: Must be freed manually, like all Godot Node types.
///   </para>
/// </remarks>
public class DataPoint : Control
{
    private Texture graphMarkerCircle;
    private Texture graphMarkerCross;
    private Texture graphMarkerSkull;

    private bool isMouseOver;

    private Vector2 coordinate;
    private float size;

    public DataPoint()
    {
        Size = 7;
        IconType = MarkerIcon.Circle;
    }

    public DataPoint(float xValue, float yValue)
    {
        Value = new Vector2(xValue, yValue);
        Size = 7;
        IconType = MarkerIcon.Circle;
    }

    public enum MarkerIcon
    {
        Circle,
        Cross,
        Skull,
    }

    /// <summary>
    ///   Visual shape of this point
    /// </summary>
    public MarkerIcon IconType { get; set; }

    /// <summary>
    ///   Actual data this point represents
    /// </summary>
    public Vector2 Value { get; set; }

    /// <summary>
    ///   Position of this point on a chart, this is different from Value.
    ///   This is automatically set in the chart-specific class.
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
    ///   Visual size of this point
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
    ///   Used to hide marker visual while still keeping it interactable
    /// </summary>
    public bool Draw { get; set; } = true;

    public override void _Ready()
    {
        graphMarkerCircle = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCircle.png");
        graphMarkerCross = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCross.png");
        graphMarkerSkull = GD.Load<Texture>("res://assets/textures/gui/bevel/SuicideIcon.png");

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
            {
                var color = MarkerColour;

                if (isMouseOver)
                    color = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerCross, new Rect2(
                    (RectSize / 2) - (vectorSize / 2), vectorSize), false, color);
                break;
            }

            case MarkerIcon.Skull:
            {
                var colour = MarkerColour;

                if (isMouseOver)
                    colour = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerSkull, new Rect2(
                    (RectSize / 2) - (vectorSize / 2), vectorSize), false, colour);
                break;
            }

            default:
                throw new Exception("Invalid marker shape");
        }
    }

    public override string ToString()
    {
        return $"Value: {Value.ToString()} Coord: {Coordinate.ToString()}";
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
