﻿using System;
using Godot;

/// <summary>
///   Data point / marker on a chart containing two immutable numerical value (x, y).
///   This inherits Control to make this interactable as well as for giving it a visual marker.
/// </summary>
/// <remarks>
///   <para>
///     Note: May cause performance overhead for chart with lots of data points.
///     Note: Must be freed manually, like all Godot Node types.
///   </para>
/// </remarks>
public class DataPoint : Control, ICloneable, IEquatable<DataPoint>
{
    public readonly double X;
    public readonly double Y;

    private Texture graphMarkerCircle = null!;
    private Texture graphMarkerCross = null!;
    private Texture graphMarkerSkull = null!;

    private bool isMouseOver;

    private Vector2 coordinate;
    private float size = 7;

    private Tween tween = new();

    public DataPoint(double x, double y)
    {
        X = x;
        Y = y;
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
    public MarkerIcon IconType { get; set; } = MarkerIcon.Circle;

    /// <summary>
    ///   The position of this point on a chart and is different from Value. This should be set in the
    ///   host chart class due to the possible calculation-specific differences in various charts.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Setting this will always tween the rect position (smoothly moving it to the set coordinate).
    ///     For more options, please use <see cref="SetCoordinate"/> method.
    ///   </para>
    /// </remarks>
    public Vector2 Coordinate
    {
        get => coordinate;
        set => SetCoordinate(value, true);
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
            UpdateRectSize();
        }
    }

    public Color MarkerFillerColour { get; set; } = new(0.0f, 0.13f, 0.14f);

    public Color MarkerFillerHighlightedColour { get; set; } = new(0.07f, 1.0f, 0.84f);

    public Color MarkerColour { get; set; }

    /// <summary>
    ///   Used to hide marker visual while still keeping it interactable
    /// </summary>
    public bool Draw { get; set; } = true;

    public static bool operator ==(DataPoint? lhs, DataPoint? rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(DataPoint? lhs, DataPoint? rhs)
    {
        return !(lhs == rhs);
    }

    public override void _Ready()
    {
        graphMarkerCircle = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCircle.png");
        graphMarkerCross = GD.Load<Texture>("res://assets/textures/gui/bevel/graphMarkerCross.png");
        graphMarkerSkull = GD.Load<Texture>("res://assets/textures/gui/bevel/SuicideIcon.png");

        AddChild(tween);

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExit));

        UpdateRectSize();
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
                    RectSize / 2 - vectorSize / 2, vectorSize), false, MarkerColour);

                break;
            }

            case MarkerIcon.Cross:
            {
                var color = MarkerColour;

                if (isMouseOver)
                    color = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerCross, new Rect2(
                    RectSize / 2 - vectorSize / 2, vectorSize), false, color);
                break;
            }

            case MarkerIcon.Skull:
            {
                var colour = MarkerColour;

                if (isMouseOver)
                    colour = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerSkull, new Rect2(
                    RectSize / 2 - vectorSize / 2, vectorSize), false, colour);
                break;
            }

            default:
                throw new Exception("Invalid marker shape");
        }
    }

    /// <summary>
    ///   This can be used rather than <see cref="Coordinate"/>'s property setter to allow for flexible
    ///   control of whether to tween the rect position or not.
    /// </summary>
    public void SetCoordinate(Vector2 target, bool useTween = true,
        Tween.TransitionType transitionType = Tween.TransitionType.Expo,
        Tween.EaseType easeType = Tween.EaseType.Out, float duration = 0.5f)
    {
        if (coordinate == target)
            return;

        coordinate = target;

        if (!useTween)
        {
            RectPosition = coordinate - RectSize / 2;
        }
        else
        {
            tween.InterpolateProperty(
                this, "rect_position", RectPosition, coordinate - RectSize / 2, duration, transitionType, easeType);
            tween.Start();
        }
    }

    public override string ToString()
    {
        return $"Value: {X}, {Y} Coord: {Coordinate}";
    }

    public object Clone()
    {
        var result = new DataPoint(X, Y)
        {
            IconType = IconType,
            Coordinate = Coordinate,
            Size = Size,
            MarkerFillerColour = MarkerFillerColour,
            MarkerFillerHighlightedColour = MarkerFillerHighlightedColour,
            MarkerColour = MarkerColour,
            Draw = Draw,
        };

        return result;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DataPoint);
    }

    public bool Equals(DataPoint? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other != null &&
            X == other.X &&
            Y == other.Y;
    }

    public override int GetHashCode()
    {
        var hashCode = 1502939027;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        return hashCode;
    }

    private void UpdateRectSize()
    {
        // Increased by 10 for a more bigger cursor detection area
        RectSize = new Vector2(size + 10, size + 10);
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
