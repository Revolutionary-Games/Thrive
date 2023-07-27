using System.Collections.Generic;
using System.Linq;
using Godot;

public class MicrobeArenaOptionsGUI : MarginContainer, IGameModeOptionsMenu
{
    [Export]
    public NodePath BiomesPath = null!;

    private OptionButton biomes = null!;

    private List<Biome>? shownBiomes;

    public override void _Ready()
    {
        biomes = GetNode<OptionButton>(BiomesPath);

        shownBiomes = SimulationParameters.Instance.GetAllBiomes().ToList();

        foreach (var biome in shownBiomes)
        {
            biomes.AddItem(biome.Name);
        }
    }

    public Vars ReadSettings()
    {
        var settings = new Vars();
        settings.SetVar("Biome", shownBiomes?[biomes.Selected].InternalName ??
            SimulationParameters.Instance.GetBiome("tidepool").InternalName);

        // TODO: Changing this requires adjusting MicrobeArena.COMPOUND_PLANE_SIZE_MAGIC_NUMBER
        settings.SetVar("Radius", 1000);

        return settings;
    }
}
