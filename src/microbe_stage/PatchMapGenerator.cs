﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Vector2 = Godot.Vector2;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Patch names aren't proper words")]
public static class PatchMapGenerator
{
    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies, Random? random = null)
    {
        // TODO: implement actual generation based on settings
        _ = settings;

        var map = new PatchMap();

        random ??= new Random();

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();
        var areaName = nameGenerator.Next(random);

        var patchCoords = new List<Vector2>();
        int [,] graph = new int [100,100];
        int vertexNr = random.Next(6,10);
        int edgeNr = random.Next(vertexNr, 2*vertexNr - 4);
        int minDistance = 80;
        for(int i = 0;i < vertexNr; i++)
        {
            int x = random.Next(30,770);
            int y = random.Next(30,770);
            var coord = new Vector2(x,y);
            bool check = true;

            check = CheckPatchDistance(coord, patchCoords, minDistance);


            while (!check)
            {
                x = random.Next(30,770);
                y = random.Next(30,770);
                coord = new Vector2(x,y);
                check = CheckPatchDistance(coord, patchCoords, minDistance);
            }

            patchCoords.Add(coord);

            GD.Print(coord);
            var patch = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), i,
            GetBiomeTemplate("aavolcanic_vent"))
            {
                Depth =
                {
                    [0] = 2500,
                    [1] = 3000,
                },
                ScreenCoordinates = coord,
            };

            map.AddPatch(patch);
            map.CurrentPatch = patch;
        }

        for (int k = 0; k < vertexNr; k++)
            if (map.Patches[k].Adjacent.Count == 0)
                for (int l = 0; l < vertexNr; l++)
                    if(l != k)
                        LinkPatches(map.Patches[k], map.Patches[l]);

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
        // TODO: remove this entire method, see: https://github.com/Revolutionary-Games/Thrive/issues/3146
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

    private static bool CheckPatchDistance(Vector2 patchCoord, List<Vector2> allPatchesCoords, int minDistance)
    {
        if (allPatchesCoords.Count == 0)
            return true;

        for (int i = 0;i< allPatchesCoords.Count; i++)
        {
            var dist = (int)patchCoord.DistanceTo(allPatchesCoords[i]);
            GD.Print(dist);
            if (dist < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    private static PatchMap PredefinedMap(PatchMap map, string areaName, Species defaultSpecies)
    {
                // Predefined patches
        var vents = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), 0,
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

        var mesopelagic = new Patch(GetPatchLocalizedName(areaName, "MESOPELAGIC"), 1,
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

        var epipelagic = new Patch(GetPatchLocalizedName(areaName, "EPIPELAGIC"), 2,
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

        var tidepool = new Patch(GetPatchLocalizedName(areaName, "TIDEPOOL"), 3,
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

        var bathypelagic = new Patch(GetPatchLocalizedName(areaName, "BATHYPELAGIC"), 4,
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

        var abyssopelagic = new Patch(GetPatchLocalizedName(areaName, "ABYSSOPELAGIC"), 5,
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

        var coast = new Patch(GetPatchLocalizedName(areaName, "COASTAL"), 6,
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

        var estuary = new Patch(GetPatchLocalizedName(areaName, "ESTUARY"), 7,
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

        var cave = new Patch(GetPatchLocalizedName(areaName, "UNDERWATERCAVE"), 8,
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

        var iceShelf = new Patch(GetPatchLocalizedName(areaName, "ICESHELF"), 9,
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

        var seafloor = new Patch(GetPatchLocalizedName(areaName, "SEA_FLOOR"), 10,
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

    private static LocalizedString GetPatchLocalizedName(string name, string biomeKey)
    {
        return new LocalizedString("PATCH_NAME", name, new LocalizedString(biomeKey));
    }
}
