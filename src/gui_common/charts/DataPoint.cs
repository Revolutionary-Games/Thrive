using System;
using System.Collections.Generic;
using Godot;

// Instances are created only through code
// ReSharper disable once Godot.MissingParameterlessConstructor
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
public partial class DataPoint : Control, ICloneable, IEquatable<DataPoint>
{
    private static readonly Stack<DataPoint> DataPointCache = new();

    private readonly NodePath positionReference = new("position");

#pragma warning disable CA2213
    private Texture2D graphMarkerCircle = null!;
    private Texture2D graphMarkerCross = null!;
    private Texture2D graphMarkerSkull = null!;

    private Tween? tween;
#pragma warning restore CA2213

    private bool isMouseOver;

    private Vector2 coordinate;
    private float size = 7;

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

    public double X { get; private set; }
    public double Y { get; private set; }

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
    public float PointSize
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
    ///   Used to hide marker (when set to <c>false</c>) visual while still keeping it interactable
    /// </summary>
    public bool DrawMarker { get; set; } = true;

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

    /// <summary>
    ///   Get a visible datapoint from the cache or a new one if the cache is empty. Due to the fact that this isn't a
    ///   constructor, all common parameters that needs to be initialized can be passed passed in as optional
    ///   parameters.
    /// </summary>
    public static DataPoint GetDataPoint(double x, double y, MarkerIcon iconType = MarkerIcon.Circle, float size = 7,
        Color markerColour = default, bool drawMarker = true, Vector2 coordinate = default)
    {
        if (DataPointCache.Count == 0)
        {
            return new DataPoint(x, y)
            {
                IconType = iconType, PointSize = size, MarkerColour = markerColour, DrawMarker = drawMarker,
                coordinate = coordinate,
            };
        }

        var point = DataPointCache.Pop();
        point.X = x;
        point.Y = y;
        point.IconType = iconType;
        point.PointSize = size;
        point.MarkerColour = markerColour;
        point.DrawMarker = drawMarker;
        point.coordinate = coordinate;
        point.Visible = true;
        return point;
    }

    /// <summary>
    ///   Returns a datapoint to the cache. Make sure that the point isn't used anywhere else before returning it to
    ///   the cache.
    /// </summary>
    public static void ReturnDataPoint(DataPoint point)
    {
        if (point.GetParent() != null)
        {
            GD.PrintErr(point.GetPath(), " still has a parent, so returning to cache has failed.");
            return;
        }

        point.StopTween();

        DataPointCache.Push(point);
    }

    public override void _Ready()
    {
        graphMarkerCircle = GD.Load<Texture2D>("res://assets/textures/gui/bevel/graphMarkerCircle.png");
        graphMarkerCross = GD.Load<Texture2D>("res://assets/textures/gui/bevel/graphMarkerCross.png");
        graphMarkerSkull = GD.Load<Texture2D>("res://assets/textures/gui/bevel/SuicideIcon.png");

        Connect(Control.SignalName.MouseEntered, new Callable(this, nameof(OnMouseEnter)));
        Connect(Control.SignalName.MouseExited, new Callable(this, nameof(OnMouseExit)));

        UpdateRectSize();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!DrawMarker)
            return;

        var vectorSize = new Vector2(PointSize, PointSize);

        switch (IconType)
        {
            case MarkerIcon.Circle:
            {
                // Circle filler
                if (isMouseOver)
                {
                    DrawCircle(Size * 0.5f, PointSize * 0.5f, MarkerFillerHighlightedColour);
                }
                else
                {
                    DrawCircle(Size * 0.5f, PointSize * 0.5f, MarkerFillerColour);
                }

                DrawTextureRect(graphMarkerCircle, new Rect2(Size * 0.5f - vectorSize * 0.5f, vectorSize), false,
                    MarkerColour);

                break;
            }

            case MarkerIcon.Cross:
            {
                var color = MarkerColour;

                if (isMouseOver)
                    color = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerCross, new Rect2(Size * 0.5f - vectorSize * 0.5f, vectorSize), false, color);
                break;
            }

            case MarkerIcon.Skull:
            {
                var colour = MarkerColour;

                if (isMouseOver)
                    colour = MarkerColour.Lightened(0.5f);

                DrawTextureRect(graphMarkerSkull, new Rect2(Size * 0.5f - vectorSize * 0.5f, vectorSize), false,
                    colour);
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
        Tween.EaseType easeType = Tween.EaseType.Out, double duration = 0.5)
    {
        if (coordinate == target)
            return;

        coordinate = target;

        if (!useTween)
        {
            Position = coordinate - Size * 0.5f;
        }
        else
        {
            StopTween();

            tween = CreateTween();
            tween.TweenProperty(this, positionReference, coordinate - Size * 0.5f, duration).SetTrans(transitionType)
                .SetEase(easeType);
        }
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

    public object Clone()
    {
        var result = new DataPoint(X, Y)
        {
            IconType = IconType,
            Coordinate = Coordinate,
            PointSize = PointSize,
            MarkerFillerColour = MarkerFillerColour,
            MarkerFillerHighlightedColour = MarkerFillerHighlightedColour,
            MarkerColour = MarkerColour,
            DrawMarker = DrawMarker,
        };

        return result;
    }

    public override int GetHashCode()
    {
        var hashCode = 1502939027;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        // ReSharper disable once StringLiteralTypo
        return $"Value: {X}, {Y} Coord: {Coordinate}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            positionReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateRectSize()
    {
        // Increased by 10 for a more bigger cursor detection area
        Size = new Vector2(size + 10, size + 10);
    }

    private void StopTween()
    {
        if (tween != null)
        {
            if (tween.IsValid())
                tween.Kill();

            tween = null;
        }
    }

    private void OnMouseEnter()
    {
        isMouseOver = true;

        QueueRedraw();
    }

    private void OnMouseExit()
    {
        isMouseOver = false;

        QueueRedraw();
    }
}
