using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Patch names aren't proper words")]
public static class PatchMapGenerator
{
    // IDs for the patches in the predefined map (not the IDs of patches in the procedural map!)
    private static readonly int CoastalId = 0;
    private static readonly int EstuaryId = 1;
    private static readonly int TidepoolId = 2;
    private static readonly int EpipelagicId = 3;
    private static readonly int MesopelagicId = 4;
    private static readonly int BathypelagicId = 5;
    private static readonly int AbyssopelagicId = 6;
    private static readonly int SeafloorId = 7;
    private static readonly int CaveId = 8;
    private static readonly int IceShelfId = 9;
    private static readonly int VentsId = 10;

    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies, Random? random = null)
    {
        random ??= new Random(settings.Seed);
        var map = new PatchMap(random);
        var predefinedMap = new PatchMap(random);

        // Return the classic map if settings require it, otherwise use it to draw the procedural map
        predefinedMap = PredefinedMap(predefinedMap, TranslationServer.Translate("PANGONIAN_REGION_NAME"));
        if (settings.MapType == WorldGenerationSettings.PatchMapType.Classic)
        {
            ConfigureStartingPatch(predefinedMap, settings, defaultSpecies,
                predefinedMap.GetPatch(VentsId), predefinedMap.GetPatch(TidepoolId), random);
            return predefinedMap;
        }

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        // Initialize the graphs random parameters
        var regionCoords = new List<Vector2>();
        int[,] graph = new int[100, 100];
        int vertexNr = random.Next(6, 10);
        int edgeNr = random.Next(vertexNr, 2 * vertexNr - 4);
        int minDistance = 180;

        var currentPatchId = 0;
        var specialRegionsId = -1;

        // Potential starting patches, which must be set by the end of the generating process
        Patch? vents = null;
        Patch? tidepool = null;

        // Create the graphs random regions
        for (int i = 0; i < vertexNr; i++)
        {
            var areaName = nameGenerator.Next(random);
            var continentName = nameGenerator.ContinentName;
            var coord = new Vector2(0, 0);

            // We must create regions containing potential starting locations, so do those first
            var regionType = vents == null ? 0 : tidepool == null ? 2 : random.Next(0, 3);

            string regionTypeName;
            switch (regionType)
            {
                case 0:
                    regionTypeName = "sea";
                    break;
                case 1:
                    regionTypeName = "ocean";
                    break;
                default:
                    regionTypeName = "continent";
                    break;
            }

            var region = new PatchRegion(i, GetPatchLocalizedName(continentName, regionTypeName),
                regionTypeName, coord);
            int numberOfPatches;

            if (regionType == 2)
            {
                // Ensure the region is non-empty if we need a tidepool
                numberOfPatches = random.Next(tidepool == null ? 1 : 0, 4);

                // All continents must have at least one coastal patch.
                Patch patch = GetPatchFromPredefinedMap(CoastalId, currentPatchId++, predefinedMap, areaName);

                region.AddPatch(patch);

                while (numberOfPatches > 0)
                {
                    // Add at least one tidepool to the map, otherwise choose randomly
                    var patchIndex = tidepool == null ? TidepoolId : random.Next(0, 3);
                    patch = GetPatchFromPredefinedMap(patchIndex, currentPatchId++, predefinedMap, areaName);
                    region.AddPatch(patch);
                    numberOfPatches--;

                    if (patchIndex == TidepoolId)
                        tidepool = patch;
                }
            }
            else
            {
                numberOfPatches = random.Next(0, 4);

                // All oceans/seas must have at least one epipelagic/ice patch and a seafloor
                Patch patch;
                if (random.Next(0, 2) == 1)
                    patch = GetPatchFromPredefinedMap(EpipelagicId, currentPatchId++, predefinedMap, areaName);
                else
                    patch = GetPatchFromPredefinedMap(IceShelfId, currentPatchId++, predefinedMap, areaName);
                region.AddPatch(patch);

                // Add the patches between surface and sea floor
                for (int patchIndex = 4; numberOfPatches > 0 && patchIndex < 7; patchIndex++, numberOfPatches--)
                {
                    patch = GetPatchFromPredefinedMap(patchIndex, currentPatchId++, predefinedMap, areaName);
                    region.AddPatch(patch);
                }

                // Add the seafloor last
                patch = GetPatchFromPredefinedMap(SeafloorId, currentPatchId++, predefinedMap, areaName);
                region.AddPatch(patch);

                // Add at least one vent to the map, otherwise chance to add a vent if this is a sea/ocean region
                if (vents == null || random.Next(0, 2) == 1)
                {
                    var ventRegion =
                        new PatchRegion(specialRegionsId--, GetPatchLocalizedName(continentName, "vents"), "vents",
                            coord);

                    vents = GetPatchFromPredefinedMap(VentsId, currentPatchId++, predefinedMap, areaName);
                    ventRegion.AddPatch(vents);
                    map.AddSpecialRegion(ventRegion);
                    LinkRegions(ventRegion, region);
                }
            }

            // Random chance to create a cave
            if (random.Next(0, 2) == 1)
            {
                var caveRegion = new PatchRegion(specialRegionsId--,
                    GetPatchLocalizedName(continentName, "UNDERWATERCAVE"), "underwater_cave", coord);
                var cavePatch = GetPatchFromPredefinedMap(CaveId, currentPatchId++, predefinedMap, areaName);
                caveRegion.AddPatch(cavePatch);
                map.AddSpecialRegion(caveRegion);
                LinkRegions(caveRegion, region);

                // Chose one random patch from the region to be linked to the underwater cave
                var patchIndex = random.Next(0, region.Patches.Count - 1);
                LinkPatches(cavePatch, region.Patches[patchIndex]);
            }

            region.BuildRegion();
            coord = GenerateCoordinates(region, map, random, minDistance);

            // We add the coordinates for the center of the region
            // since that's the point that will be connected
            regionCoords.Add(coord + region.GetSize() / 2f);
            map.AddRegion(region);
        }

        // After building the normal regions we build the special ones and the patches
        map.BuildPatchesInRegions();
        map.BuildSpecialRegions();
        map.BuildPatchesInSpecialRegions();

        if (vents == null)
            throw new InvalidOperationException("No vent patch created");

        if (tidepool == null)
            throw new InvalidOperationException("No tidepool patch created");

        ConfigureStartingPatch(map, settings, defaultSpecies, vents, tidepool, random);

        // We make the graph by subtracting edges from its Delaunay Triangulation
        // as long as the graph stays connected.
        graph = DelaunayTriangulation(graph, regionCoords);
        graph = SubtractEdges(graph, vertexNr, edgeNr, random);

        // Link regions according to the graph matrix
        for (int k = 0; k < vertexNr; k++)
        {
            for (int l = 0; l < vertexNr; l++)
            {
                if (graph[l, k] == 1)
                    LinkRegions(map.Regions[k], map.Regions[l]);
            }
        }

        map.ConnectPatchesBetweenRegions();
        map.CreateAdjacenciesFromPatchData();
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

    private static void LinkRegions(PatchRegion region1, PatchRegion region2)
    {
        region1.AddNeighbour(region2);
        region2.AddNeighbour(region1);
    }

    private static int[,] SubtractEdges(int[,] graph, int vertexNr, int edgeNr,
        Random random)
    {
        var currentEdgeNr = CurrentEdgeNumber(graph, vertexNr);

        // Subtract edges until we reach the desired edge count.
        while (currentEdgeNr > edgeNr)
        {
            int edgeToDelete = random.Next(1, currentEdgeNr);
            int i;
            int j;
            for (i = 0, j = 0; i < vertexNr && edgeToDelete != 0; i++)
            {
                for (j = 0; j < vertexNr && edgeToDelete != 0 && j <= i; j++)
                {
                    if (graph[i, j] == 1)
                        edgeToDelete--;
                }
            }

            i--;
            j--;

            // Check if the graph stays connected after subtracting the edge
            // otherwise, leave the edge as is.
            graph[i, j] = graph[j, i] = 0;
            if (!CheckConnectivity(graph, vertexNr))
                graph[i, j] = graph[j, i] = 1;
            else
                currentEdgeNr -= 1;
        }

        return graph;
    }

    // Create a triangulation for a certain graph given some vertex coordinates
    private static int[,] DelaunayTriangulation(int[,] graph, List<Vector2> vertexCoords)
    {
        var indices = Geometry.TriangulateDelaunay2d(vertexCoords.ToArray());
        var triangles = indices.ToList();
        for (int i = 0; i < triangles.Count - 2; i += 3)
        {
            graph[triangles[i], triangles[i + 1]] = graph[triangles[i + 1], triangles[i]] = 1;
            graph[triangles[i + 1], triangles[i + 2]] = graph[triangles[i + 2], triangles[i + 1]] = 1;
            graph[triangles[i], triangles[i + 2]] = graph[triangles[i + 2], triangles[i]] = 1;
        }

        return graph;
    }

    // DFS graph search
    private static int[] Dfs(int[,] graph, int vertexNr, int point, int[] visited)
    {
        visited[point] = 1;
        for (int i = 0; i < vertexNr; i++)
        {
            if (graph[point, i] == 1 && visited[i] == 0)
                visited = Dfs(graph, vertexNr, i, visited);
        }

        return visited;
    }

    // Checks the graphs connectivity
    private static bool CheckConnectivity(int[,] graph, int vertexNr)
    {
        int[] visited = new int[vertexNr];
        visited = Dfs(graph, vertexNr, 0, visited);
        if (visited.Sum() != vertexNr)
            return false;

        return true;
    }

    // Current number of edges in a given graph
    private static int CurrentEdgeNumber(int[,] graph, int vertexNr)
    {
        int edgeNr = 0;
        for (int i = 0; i < vertexNr; i++)
        {
            for (int j = 0; j < vertexNr; j++)
            {
                edgeNr += graph[i, j];
            }
        }

        return edgeNr / 2;
    }

    // Checks distance between patches
    private static bool CheckRegionDistance(PatchRegion region, PatchMap map, int minDistance)
    {
        if (map.Regions.Count == 0)
            return true;

        for (int i = 0; i < map.Regions.Count; i++)
        {
            if (CheckIfRegionsIntersect(region, map.Regions[i], minDistance))
                return false;
        }

        return true;
    }

    private static bool CheckIfRegionsIntersect(PatchRegion region1, PatchRegion region2, int minDistance)
    {
        var minDist = new Vector2(minDistance, minDistance);
        var region1Rect = new Rect2(region1.ScreenCoordinates, region1.GetSize() + minDist);
        var region2Rect = new Rect2(region2.ScreenCoordinates, region2.GetSize() + minDist);
        return region1Rect.Intersects(region2Rect, true);
    }

    private static Vector2 GenerateCoordinates(PatchRegion region, PatchMap map, Random random, int minDistance)
    {
        int x = random.Next(280, 1600);
        int y = random.Next(280, 1600);
        var coord = new Vector2(x, y);
        region.ScreenCoordinates = coord;

        // Check if the region doesnt overlap over other regions
        bool check = CheckRegionDistance(region, map, minDistance);
        while (!check)
        {
            x = random.Next(280, 1600);
            y = random.Next(280, 1600);
            coord = new Vector2(x, y);
            region.ScreenCoordinates = coord;
            check = CheckRegionDistance(region, map, minDistance);
        }

        return coord;
    }

    private static Patch GetPatchFromPredefinedMap(int patchId, int newId, PatchMap predefinedMap, string areaName)
    {
        var patch = predefinedMap.Patches[patchId];
        patch = new Patch(GetPatchLocalizedName(areaName, patch.BiomeTemplate.Name), newId, patch.BiomeTemplate,
            patch.Region)
        {
            Depth =
            {
                [0] = patch.Depth[0],
                [1] = patch.Depth[1],
            },
        };

        return patch;
    }

    private static PatchMap PredefinedMap(PatchMap map, string areaName)
    {
        var region = new PatchRegion(0, GetPatchLocalizedName(areaName, string.Empty), string.Empty, new Vector2(0, 0));

        // Predefined patches
        var coast = new Patch(GetPatchLocalizedName(areaName, "COASTAL"), CoastalId,
            GetBiomeTemplate("coastal"), region)
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(100, 100),
        };
        region.AddPatch(coast);

        var estuary = new Patch(GetPatchLocalizedName(areaName, "ESTUARY"), EstuaryId,
            GetBiomeTemplate("estuary"), region)
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(70, 160),
        };
        region.AddPatch(estuary);

        var tidepool = new Patch(GetPatchLocalizedName(areaName, "TIDEPOOL"), TidepoolId,
            GetBiomeTemplate("tidepool"), region)
        {
            Depth =
            {
                [0] = 0,
                [1] = 10,
            },
            ScreenCoordinates = new Vector2(300, 100),
        };
        region.AddPatch(tidepool);

        var epipelagic = new Patch(GetPatchLocalizedName(areaName, "EPIPELAGIC"), EpipelagicId,
            GetBiomeTemplate("default"), region)
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 100),
        };
        region.AddPatch(epipelagic);

        var mesopelagic = new Patch(GetPatchLocalizedName(areaName, "MESOPELAGIC"), MesopelagicId,
            GetBiomeTemplate("mesopelagic"), region)
        {
            Depth =
            {
                [0] = 200,
                [1] = 1000,
            },
            ScreenCoordinates = new Vector2(200, 200),
        };
        region.AddPatch(mesopelagic);

        var bathypelagic = new Patch(GetPatchLocalizedName(areaName, "BATHYPELAGIC"), BathypelagicId,
            GetBiomeTemplate("bathypelagic"), region)
        {
            Depth =
            {
                [0] = 1000,
                [1] = 4000,
            },
            ScreenCoordinates = new Vector2(200, 300),
        };
        region.AddPatch(bathypelagic);

        var abyssopelagic = new Patch(GetPatchLocalizedName(areaName, "ABYSSOPELAGIC"), AbyssopelagicId,
            GetBiomeTemplate("abyssopelagic"), region)
        {
            Depth =
            {
                [0] = 4000,
                [1] = 6000,
            },
            ScreenCoordinates = new Vector2(300, 400),
        };
        region.AddPatch(abyssopelagic);

        var seafloor = new Patch(GetPatchLocalizedName(areaName, "SEA_FLOOR"), SeafloorId,
            GetBiomeTemplate("seafloor"), region)
        {
            Depth =
            {
                [0] = 4000,
                [1] = 6000,
            },
            ScreenCoordinates = new Vector2(200, 400),
        };
        region.AddPatch(seafloor);

        var cave = new Patch(GetPatchLocalizedName(areaName, "UNDERWATERCAVE"), CaveId,
            GetBiomeTemplate("underwater_cave"), region)
        {
            Depth =
            {
                [0] = 200,
                [1] = 1000,
            },
            ScreenCoordinates = new Vector2(300, 200),
        };
        region.AddPatch(cave);

        var iceShelf = new Patch(GetPatchLocalizedName(areaName, "ICESHELF"), IceShelfId,
            GetBiomeTemplate("ice_shelf"), region)
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 30),
        };
        region.AddPatch(iceShelf);

        var vents = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), VentsId,
            GetBiomeTemplate("aavolcanic_vent"), region)
        {
            Depth =
            {
                [0] = 2500,
                [1] = 3000,
            },
            ScreenCoordinates = new Vector2(100, 400),
        };
        region.AddPatch(vents);

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

        map.AddRegion(region);
        map.BuildPatchesInRegions();
        return map;
    }

    private static void ConfigureStartingPatch(PatchMap map, WorldGenerationSettings settings, Species defaultSpecies,
        Patch vents, Patch tidepool, Random random)
    {
        // Choose this here to ensure the same seed creates the same world regardless of starting location
        var randomPatch = map.Patches[random.Next(0, map.Patches.Count)];

        switch (settings.Origin)
        {
            case WorldGenerationSettings.LifeOrigin.Vent:
                map.CurrentPatch = vents;
                break;
            case WorldGenerationSettings.LifeOrigin.Pond:
                map.CurrentPatch = tidepool;
                break;
            case WorldGenerationSettings.LifeOrigin.Panspermia:
                map.CurrentPatch = randomPatch;
                break;
            default:
                GD.PrintErr($"Selected origin {settings.Origin} doesn't match a known origin type");
                map.CurrentPatch = randomPatch;
                break;
        }

        map.CurrentPatch.AddSpecies(defaultSpecies);
    }

    private static LocalizedString GetPatchLocalizedName(string name, string biomeKey)
    {
        return new LocalizedString("PATCH_NAME", name, new LocalizedString(biomeKey));
    }
}
