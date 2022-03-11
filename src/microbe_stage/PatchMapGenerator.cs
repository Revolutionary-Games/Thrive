using System.Diagnostics.CodeAnalysis;
using Godot;
using Newtonsoft.Json;
using File = Godot.File;

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
        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();
        var name = nameGenerator.Next();

        // Predefined patches
        var vents = new Patch(name+" Vents", 0,
            GetBiomeTemplate("aavolcanic_vent"))
        {
            Depth =
            {
                [0] = 2500,
                [1] = 3000,
            },
            ScreenCoordinates = new Vector2(100, 400),
        };
        vents.AddSpecies(defaultSpecies);
        map.AddPatch(vents);

        var mesopelagic = new Patch(name+" Mesopelagic", 1,
            GetBiomeTemplate("mesopelagic"))
        {
            Depth =
            {
                [0] = 200,
                [1] = 1000,
            },
            ScreenCoordinates = new Vector2(200, 200),
        };
        map.AddPatch(mesopelagic);

        var epipelagic = new Patch(name+" Epipelagic", 2,
            GetBiomeTemplate("default"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 100),
        };
        map.AddPatch(epipelagic);

        var tidepool = new Patch(name+" Tidepool", 3,
            GetBiomeTemplate("tidepool"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 10,
            },
            ScreenCoordinates = new Vector2(300, 100),
        };
        map.AddPatch(tidepool);

        var bathypelagic = new Patch(name+" Bathypelagic", 4,
            GetBiomeTemplate("bathypelagic"))
        {
            Depth =
            {
                [0] = 1000,
                [1] = 4000,
            },
            ScreenCoordinates = new Vector2(200, 300),
        };
        map.AddPatch(bathypelagic);

        var abyssopelagic = new Patch(name+" Abyssopelagic", 5,
            GetBiomeTemplate("abyssopelagic"))
        {
            Depth =
            {
                [0] = 4000,
                [1] = 6000,
            },
            ScreenCoordinates = new Vector2(300, 400),
        };
        map.AddPatch(abyssopelagic);

        var coast = new Patch(name+" Coast", 6,
            GetBiomeTemplate("coastal"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(100, 100),
        };
        map.AddPatch(coast);

        var estuary = new Patch(name+" Estuary", 7,
            GetBiomeTemplate("estuary"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(70, 160),
        };
        map.AddPatch(estuary);

        var cave = new Patch("PATCH_CAVE", 8,
            GetBiomeTemplate("underwater_cave"))
        {
            Depth =
            {
                [0] = 200,
                [1] = 1000,
            },
            ScreenCoordinates = new Vector2(300, 200),
        };
        map.AddPatch(cave);

        var iceShelf = new Patch("PATCH_ICE_SHELF", 9,
            GetBiomeTemplate("ice_shelf"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 30),
        };
        map.AddPatch(iceShelf);

        var seafloor = new Patch(name+" Seafloor", 10,
            GetBiomeTemplate("seafloor"))
        {
            Depth =
            {
                [0] = 4000,
                [1] = 6000,
            },
            ScreenCoordinates = new Vector2(200, 400),
        };
        map.AddPatch(seafloor);

        // Connections
        LinkPatches(vents, seafloor);
        LinkPatches(seafloor, bathypelagic);
        LinkPatches(seafloor, abyssopelagic);
        LinkPatches(bathypelagic, abyssopelagic);
        LinkPatches(bathypelagic, mesopelagic);
        LinkPatches(mesopelagic, epipelagic);
        LinkPatches(mesopelagic, cave);
        LinkPatches(epipelagic, tidepool);
        LinkPatches(epipelagic, iceShelf);
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
