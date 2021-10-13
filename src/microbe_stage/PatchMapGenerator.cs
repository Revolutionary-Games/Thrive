using System.Diagnostics.CodeAnalysis;
using Godot;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Patch names aren't proper words")]
public static class PatchMapGenerator
{
    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies)
    {
        // TODO: implement actual generation based on settings
        _ = settings;

        var map = new PatchMap();

        // Predefined patches
        Patch vents = new Patch("PATCH_PANGONIAN_VENTS", 0,
            GetBiomeTemplate("aavolcanic_vent"));
        vents.Depth[0] = 2500;
        vents.Depth[1] = 3000;
        vents.AddSpecies(defaultSpecies);
        vents.ScreenCoordinates = new Vector2(100, 400);
        map.AddPatch(vents);

        Patch mesopelagic = new Patch("PATCH_PANGONIAN_MESOPELAGIC", 1,
            GetBiomeTemplate("mesopelagic"));
        mesopelagic.Depth[0] = 200;
        mesopelagic.Depth[1] = 1000;
        mesopelagic.ScreenCoordinates = new Vector2(200, 200);
        map.AddPatch(mesopelagic);

        Patch epipelagic = new Patch("PATCH_PANGONIAN_EPIPELAGIC", 2,
            GetBiomeTemplate("default"));
        epipelagic.Depth[0] = 0;
        epipelagic.Depth[1] = 200;
        epipelagic.ScreenCoordinates = new Vector2(200, 100);
        map.AddPatch(epipelagic);

        Patch tidepool = new Patch("PATCH_PANGONIAN_TIDEPOOL", 3,
            GetBiomeTemplate("tidepool"));
        tidepool.Depth[0] = 0;
        tidepool.Depth[1] = 10;
        tidepool.ScreenCoordinates = new Vector2(300, 100);
        map.AddPatch(tidepool);

        Patch bathypelagic = new Patch("PATCH_PANGONIAN_BATHYPELAGIC", 4,
            GetBiomeTemplate("bathypelagic"));
        bathypelagic.Depth[0] = 1000;
        bathypelagic.Depth[1] = 4000;
        bathypelagic.ScreenCoordinates = new Vector2(200, 300);
        map.AddPatch(bathypelagic);

        Patch abyssopelagic = new Patch("PATHCH_PANGONIAN_ABYSSOPELAGIC", 5,
            GetBiomeTemplate("abyssopelagic"));
        abyssopelagic.Depth[0] = 4000;
        abyssopelagic.Depth[1] = 6000;
        abyssopelagic.ScreenCoordinates = new Vector2(300, 400);
        map.AddPatch(abyssopelagic);

        Patch coast = new Patch("PATCH_PANGONIAN_COAST", 6,
            GetBiomeTemplate("coastal"));
        coast.Depth[0] = 0;
        coast.Depth[1] = 200;
        coast.ScreenCoordinates = new Vector2(100, 100);
        map.AddPatch(coast);

        Patch estuary = new Patch("PATCH_PANGONIAN_ESTUARY", 7,
            GetBiomeTemplate("estuary"));
        estuary.Depth[0] = 0;
        estuary.Depth[1] = 200;
        estuary.ScreenCoordinates = new Vector2(70, 160);
        map.AddPatch(estuary);

        Patch cave = new Patch("PATCH_CAVE", 8,
            GetBiomeTemplate("underwater_cave"));
        cave.Depth[0] = 200;
        cave.Depth[1] = 1000;
        cave.ScreenCoordinates = new Vector2(300, 200);
        map.AddPatch(cave);

        Patch iceshelf = new Patch("PATCH_ICE_SHELF", 9,
            GetBiomeTemplate("ice_shelf"));
        iceshelf.Depth[0] = 0;
        iceshelf.Depth[1] = 200;
        iceshelf.ScreenCoordinates = new Vector2(200, 30);
        map.AddPatch(iceshelf);

        Patch seafloor = new Patch("PATCH_PANGONIAN_SEAFLOOR", 10,
            GetBiomeTemplate("seafloor"));
        seafloor.Depth[0] = 4000;
        seafloor.Depth[1] = 6000;
        seafloor.ScreenCoordinates = new Vector2(200, 400);
        map.AddPatch(seafloor);

        // Connections
        LinkPatches(vents, seafloor);
        LinkPatches(seafloor, bathypelagic);
        LinkPatches(seafloor, abyssopelagic);
        LinkPatches(bathypelagic, mesopelagic);
        LinkPatches(mesopelagic, epipelagic);
        LinkPatches(mesopelagic, cave);
        LinkPatches(epipelagic, tidepool);
        LinkPatches(epipelagic, iceshelf);
        LinkPatches(epipelagic, coast);
        LinkPatches(coast, estuary);

        map.CurrentPatch = vents;
        return map;
    }

    private static Biome GetBiomeTemplate(string name)
    {
        return SimulationParameters.Instance.GetBiome(name);
    }

    private static void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }

    private static void TranslatePatchNames()
    {
        _ = TranslationServer.Translate("PATCH_PANGONIAN_VENTS");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_MESOPELAGIC");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_EPIPELAGIC");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_TIDEPOOL");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_BATHYPELAGIC");
        _ = TranslationServer.Translate("PATHCH_PANGONIAN_ABYSSOPELAGIC");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_COAST");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_ESTUARY");
        _ = TranslationServer.Translate("PATCH_CAVE");
        _ = TranslationServer.Translate("PATCH_ICE_SHELF");
        _ = TranslationServer.Translate("PATCH_PANGONIAN_SEAFLOOR");
    }
}
