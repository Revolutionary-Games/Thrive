using System;
using Godot;

/// <summary>
///   Point / marker on a line chart containing a single value data
/// </summary>
public class ChartPoint : Control
{
    private readonly Texture graphMarkerCircle =
        GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCircle.png");

    private readonly Texture graphMarkerCross =
        GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCross.png");

    private bool isMouseOver;

    private Vector2 coordinate;
    private float size;

    private TextureRect icon;

    public ChartPoint(float xValue, float yValue, float size = 8f, MarkerIcon shape = MarkerIcon.Circle)
    {
        Value = new Vector2(xValue, yValue);
        Size = size;
        IconType = shape;
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
    ///   Position of the point on the chart, this is different from value
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

            // Increased by 10 for bigger mouse detection area
            RectSize = new Vector2(value + 10, value + 10);
        }
    }

    public Color MarkerColor { get; set; }

    public override void _Ready()
    {
        icon = new TextureRect();
        icon.Expand = true;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(icon);

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExit));

        Update();
    }

    public override void _Draw()
    {
        icon.Modulate = MarkerColor;
        icon.RectSize = new Vector2(Size, Size);
        icon.RectPosition = (RectSize / 2) - (icon.RectSize / 2);

        switch (IconType)
        {
            case MarkerIcon.Circle:
            {
                // Circle filler
                if (isMouseOver)
                {
                    DrawCircle(RectSize / 2, Size / 2, new Color(0.07f, 1.0f, 0.84f));
                }
                else
                {
                    DrawCircle(RectSize / 2, Size / 2, new Color(0.0f, 0.13f, 0.14f));
                }

                icon.Texture = graphMarkerCircle;
                break;
            }

            case MarkerIcon.Cross:
                icon.Texture = graphMarkerCross;
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
