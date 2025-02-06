using AngleSharp.Dom;
using Godot;

/// <summary>
///   Dots scattered around patch map nodes, indication population of player species
/// </summary>
public partial class PatchMapPopulationIndicator : Control
{
    private int positionModifier;

#pragma warning disable CA2213
    [Export]
    private TextureRect indicator = null!;
#pragma warning restore CA2213

    public int IndicatorPositionModifier
    {
        get
        {
            return positionModifier;
        }
        set
        {
            positionModifier = value;
        }
    }

    public void UpdateIndicator(Control parent)
    {
        var nodeModifier = parent.Position.LengthSquared();
        var modifierSinus = Mathf.Sin(IndicatorPositionModifier);

        indicator.Position = Size * 0.5f + new Vector2(0, 40).Rotated(nodeModifier * 20) + new Vector2(0, modifierSinus * 50).Rotated(
            IndicatorPositionModifier * 6 * modifierSinus + nodeModifier);
    }
}
