using Godot;

/// <summary>
///   Shows a resource icon in the food chain view
/// </summary>
public partial class FoodChainResource : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    private TextureRect texture = null!;
#pragma warning restore CA2213

    private Compound displayedCompound;

    public Compound CompoundIcon
    {
        get => displayedCompound;
        set
        {
            if (displayedCompound == value)
                return;

            displayedCompound = value;

            if (displayedCompound == Compound.Invalid)
            {
                texture.Texture = null;
                return;
            }

            var definition = SimulationParameters.GetCompound(displayedCompound);
            texture.Texture = definition.LoadedIcon;

            if (texture.Texture == null)
                GD.PrintErr("Compound icon is not loaded");

            TooltipText = definition.GetUntranslatedName();
        }
    }
}
