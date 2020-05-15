using Godot;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
public static class PatchMapGenerator
{
    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies)
    {
        // TODO: implement actual generation based on settings
        _ = settings;

        var map = new PatchMap();

        // Predefined patches
        Patch patch0 = new Patch("Pangonian vents", 0, GetBiomeTemplate("aavolcanic_vent"));
        patch0.AddSpecies(defaultSpecies);
        patch0.ScreenCoordinates = new Vector2(100, 400);
        map.AddPatch(patch0);

        Patch patch1 = new Patch("Pangonian Mesopelagic", 1, GetBiomeTemplate("mesopelagic"));
        patch1.ScreenCoordinates = new Vector2(200, 200);
        map.AddPatch(patch1);

        Patch patch2 = new Patch("Pangonian Epipelagic", 2, GetBiomeTemplate("default"));
        patch2.ScreenCoordinates = new Vector2(200, 100);
        map.AddPatch(patch2);

        Patch patch3 = new Patch("Pangonian Tidepool", 3, GetBiomeTemplate("tidepool"));
        patch3.ScreenCoordinates = new Vector2(300, 100);
        map.AddPatch(patch3);

        Patch patch4 = new Patch("Pangonian Bathypalagic", 4,
            GetBiomeTemplate("bathypalagic"));
        patch4.ScreenCoordinates = new Vector2(200, 300);
        map.AddPatch(patch4);

        Patch patch5 = new Patch("Pangonian Abyssopelagic", 5,
            GetBiomeTemplate("abyssopelagic"));
        patch5.ScreenCoordinates = new Vector2(300, 400);
        map.AddPatch(patch5);

        Patch patch6 = new Patch("Pangonian Coast", 6, GetBiomeTemplate("coastal"));
        patch6.ScreenCoordinates = new Vector2(100, 100);
        map.AddPatch(patch6);

        Patch patch7 = new Patch("Pangonian Estuary", 7, GetBiomeTemplate("estuary"));
        patch7.ScreenCoordinates = new Vector2(70, 160);
        map.AddPatch(patch7);

        Patch patch8 = new Patch("Cave", 8, GetBiomeTemplate("underwater_cave"));
        patch8.ScreenCoordinates = new Vector2(300, 200);
        map.AddPatch(patch8);

        Patch patch9 = new Patch("Ice Shelf", 9, GetBiomeTemplate("ice_shelf"));
        patch9.ScreenCoordinates = new Vector2(200, 30);
        map.AddPatch(patch9);

        Patch patch10 = new Patch("Pangonian Sea Floor", 10, GetBiomeTemplate("seafloor"));
        patch10.ScreenCoordinates = new Vector2(200, 400);
        map.AddPatch(patch10);

        // Connections
        patch0.AddNeighbour(patch10);

        patch1.AddNeighbour(patch4);
        patch1.AddNeighbour(patch2);
        patch1.AddNeighbour(patch8);

        patch2.AddNeighbour(patch1);
        patch2.AddNeighbour(patch3);
        patch2.AddNeighbour(patch6);
        patch2.AddNeighbour(patch9);

        patch3.AddNeighbour(patch2);

        patch4.AddNeighbour(patch5);
        patch4.AddNeighbour(patch1);
        patch4.AddNeighbour(patch10);

        patch5.AddNeighbour(patch10);
        patch5.AddNeighbour(patch4);

        patch6.AddNeighbour(patch2);
        patch6.AddNeighbour(patch7);

        patch7.AddNeighbour(patch6);

        patch8.AddNeighbour(patch1);

        patch9.AddNeighbour(patch2);

        patch10.AddNeighbour(patch4);
        patch10.AddNeighbour(patch5);
        patch10.AddNeighbour(patch0);

        map.CurrentPatch = patch0;
        return map;
    }

    private static Biome GetBiomeTemplate(string name)
    {
        return SimulationParameters.Instance.GetBiome(name);
    }
}
