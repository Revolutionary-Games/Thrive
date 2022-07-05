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
    // IDs for the patches in the predefined map
    private enum PredefinedBiome
    {
        Coastal = 0,
        Estuary = 1,
        Tidepool = 2,
        Epipelagic = 3,
        Mesopelagic = 4,
        Bathypelagic = 5,
        Abyssopelagic = 6,
        Seafloor = 7,
        Cave = 8,
        IceShelf = 9,
        Vents = 10,
    }

    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies, Random? random = null)
    {
        random ??= new Random(settings.Seed);
        var map = new PatchMap(random);

        if (settings.MapType == WorldGenerationSettings.PatchMapType.Classic)
        {
            // Return the classic map if settings require it, otherwise use it to draw the procedural map
            var predefinedMap =
                PredefinedMap(new PatchMap(random), TranslationServer.Translate("PANGONIAN_REGION_NAME"));

            ConfigureStartingPatch(predefinedMap, settings, defaultSpecies,
                predefinedMap.GetPatch((int)PredefinedBiome.Vents),
                predefinedMap.GetPatch((int)PredefinedBiome.Tidepool), random);
            return predefinedMap;
        }

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        // Initialize the graphs random parameters
        var regionCoords = new List<Vector2>();
        int vertexNumber = random.Next(6, 10);
        int edgeNumber = random.Next(vertexNumber, 2 * vertexNumber - 4);
        int minDistance = 180;

        var currentPatchId = 0;
        var specialRegionsId = -1;

        // Potential starting patches, which must be set by the end of the generating process
        Patch? vents = null;
        Patch? tidepool = null;

        // Create the graphs random regions
        for (int i = 0; i < vertexNumber; i++)
        {
            var areaName = nameGenerator.Next(random);
            var continentName = nameGenerator.ContinentName;
            var coord = new Vector2(0, 0);

            // We must create regions containing potential starting locations, so do those first
            var regionType = vents == null ? 0 : tidepool == null ? 2 : random.Next(0, 3);

            PatchRegion.RegionType regionTypeName;
            switch (regionType)
            {
                case 0:
                    regionTypeName = PatchRegion.RegionType.Sea;
                    break;
                case 1:
                    regionTypeName = PatchRegion.RegionType.Ocean;
                    break;
                default:
                    regionTypeName = PatchRegion.RegionType.Continent;
                    break;
            }

            var region = new PatchRegion(i, GetPatchLocalizedName(continentName, regionTypeName.ToString()),
                regionTypeName, coord);
            int numberOfPatches;

            if (regionType == 2)
            {
                // Ensure the region is non-empty if we need a tidepool
                numberOfPatches = random.Next(tidepool == null ? 1 : 0, 4);

                // All continents must have at least one coastal patch.
                NewPredefinedPatch(PredefinedBiome.Coastal, currentPatchId++, region, areaName);

                while (numberOfPatches > 0)
                {
                    // Add at least one tidepool to the map, otherwise choose randomly
                    var patchIndex = tidepool == null ? PredefinedBiome.Tidepool : (PredefinedBiome)random.Next(0, 3);
                    var patch = NewPredefinedPatch(patchIndex, currentPatchId++, region, areaName);
                    numberOfPatches--;

                    if (patchIndex == PredefinedBiome.Tidepool)
                        tidepool = patch;
                }
            }
            else
            {
                numberOfPatches = random.Next(0, 4);

                // All oceans/seas must have at least one epipelagic/ice patch and a seafloor
                NewPredefinedPatch(random.Next(0, 2) == 1 ? PredefinedBiome.Epipelagic : PredefinedBiome.IceShelf,
                    currentPatchId++, region, areaName);

                // Add the patches between surface and sea floor
                for (int patchIndex = 4; numberOfPatches > 0 && patchIndex < 7; patchIndex++, numberOfPatches--)
                {
                    NewPredefinedPatch((PredefinedBiome)patchIndex, currentPatchId++, region, areaName);
                }

                // Add the seafloor last
                NewPredefinedPatch(PredefinedBiome.Seafloor, currentPatchId++, region, areaName);

                // Add at least one vent to the map, otherwise chance to add a vent if this is a sea/ocean region
                if (vents == null || random.Next(0, 2) == 1)
                {
                    var ventRegion = new PatchRegion(specialRegionsId--,
                        GetPatchLocalizedName(continentName, "vents"), PatchRegion.RegionType.Vent, coord);

                    vents = NewPredefinedPatch(PredefinedBiome.Vents, currentPatchId++, ventRegion, areaName);

                    map.AddSpecialRegion(ventRegion);
                    LinkRegions(ventRegion, region);
                }
            }

            // Random chance to create a cave
            if (random.Next(0, 2) == 1)
            {
                var caveRegion = new PatchRegion(specialRegionsId--,
                    GetPatchLocalizedName(continentName, "UNDERWATERCAVE"), PatchRegion.RegionType.Cave, coord);

                var cavePatch = NewPredefinedPatch(PredefinedBiome.Cave, currentPatchId++, caveRegion, areaName);

                map.AddSpecialRegion(caveRegion);
                LinkRegions(caveRegion, region);

                // Chose one random patch from the region to be linked to the underwater cave
                var patchIndex = random.Next(0, region.Patches.Count - 1);
                LinkPatches(cavePatch, region.Patches[patchIndex]);
            }

            BuildRegion(region);
            coord = GenerateCoordinates(region, map, random, minDistance);

            // We add the coordinates for the center of the region
            // since that's the point that will be connected
            regionCoords.Add(coord + region.Size / 2.0f);
            map.AddRegion(region);
        }

        // After building the normal regions we build the special ones and the patches
        BuildPatchesInRegions(map);
        BuildSpecialRegions(map);
        BuildPatchesInSpecialRegions(map);

        if (vents == null)
            throw new InvalidOperationException("No vent patch created");

        if (tidepool == null)
            throw new InvalidOperationException("No tidepool patch created");

        ConfigureStartingPatch(map, settings, defaultSpecies, vents, tidepool, random);

        // We make the graph by subtracting edges from its Delaunay Triangulation
        // as long as the graph stays connected.
        int[,] graph = DelaunayTriangulation(new int[100, 100], regionCoords);
        graph = SubtractEdges(graph, vertexNumber, edgeNumber, random);

        // Link regions according to the graph matrix
        for (int k = 0; k < vertexNumber; k++)
        {
            for (int l = 0; l < vertexNumber; l++)
            {
                if (graph[l, k] == 1)
                    LinkRegions(map.Regions[k], map.Regions[l]);
            }
        }

        ConnectPatchesBetweenRegions(map);
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

    private static int[,] SubtractEdges(int[,] graph, int vertexNumber, int edgeNumber,
        Random random)
    {
        var currentEdgeNumber = CurrentEdgeNumber(graph, vertexNumber);

        // Subtract edges until we reach the desired edge count.
        while (currentEdgeNumber > edgeNumber)
        {
            int edgeToDelete = random.Next(1, currentEdgeNumber);
            int i;
            int j;
            for (i = 0, j = 0; i < vertexNumber && edgeToDelete != 0; i++)
            {
                for (j = 0; j < vertexNumber && edgeToDelete != 0 && j <= i; j++)
                {
                    if (graph[i, j] == 1)
                        edgeToDelete--;
                }
            }

            --i;
            --j;

            // Check if the graph stays connected after subtracting the edge
            // otherwise, leave the edge as is.
            graph[i, j] = graph[j, i] = 0;

            if (!CheckConnectivity(graph, vertexNumber))
            {
                graph[i, j] = graph[j, i] = 1;
            }
            else
            {
                currentEdgeNumber -= 1;
            }
        }

        return graph;
    }

    /// <summary>
    ///   Create a triangulation for a certain graph given some vertex coordinates
    /// </summary>
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

    /// <summary>
    ///   DFS graph traversal to get all connected nodes
    /// </summary>
    /// <param name="graph">The graph to test</param>
    /// <param name="vertexNumber">Count of vertexes</param>
    /// <param name="point">Current point</param>
    /// <param name="visited">Array of already visited vertexes</param>
    private static void DeepFirstGraphTraversal(int[,] graph, int vertexNumber, int point, ref int[] visited)
    {
        visited[point] = 1;

        for (var i = 0; i < vertexNumber; i++)
        {
            if (graph[point, i] == 1 && visited[i] == 0)
                DeepFirstGraphTraversal(graph, vertexNumber, i, ref visited);
        }
    }

    // Checks the graphs connectivity
    private static bool CheckConnectivity(int[,] graph, int vertexNumber)
    {
        int[] visited = new int[vertexNumber];
        DeepFirstGraphTraversal(graph, vertexNumber, 0, ref visited);
        return visited.Sum() == vertexNumber;
    }

    // Current number of edges in a given graph
    private static int CurrentEdgeNumber(int[,] graph, int vertexNumber)
    {
        int edgeNumber = 0;
        for (int i = 0; i < vertexNumber; i++)
        {
            for (int j = 0; j < vertexNumber; j++)
            {
                edgeNumber += graph[i, j];
            }
        }

        return edgeNumber / 2;
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
        var region1Rect = new Rect2(region1.ScreenCoordinates, region1.Size + minDist);
        var region2Rect = new Rect2(region2.ScreenCoordinates, region2.Size + minDist);
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

    private static void ConnectPatchesBetweenRegions(PatchRegion region, Random random)
    {
        if (region.Type is PatchRegion.RegionType.Ocean or PatchRegion.RegionType.Sea)
        {
            foreach (var adjacent in region.Adjacent)
            {
                if (adjacent.Type == PatchRegion.RegionType.Continent)
                {
                    var patchIndex = random.Next(0, adjacent.Patches.Count - 1);
                    LinkPatches(region.Patches[0], adjacent.Patches[patchIndex]);
                }

                if (adjacent.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean)
                {
                    int lowestConnectedLevel;
                    lowestConnectedLevel = Math.Min(region.Patches.Count, adjacent.Patches.Count);
                    lowestConnectedLevel = random.Next(0, lowestConnectedLevel - 1);

                    for (int i = 0; i <= lowestConnectedLevel; i++)
                    {
                        LinkPatches(region.Patches[i], adjacent.Patches[i]);
                    }
                }
            }
        }

        if (region.Type == PatchRegion.RegionType.Continent)
        {
            foreach (var adjacent in region.Adjacent)
            {
                if (adjacent.Type == PatchRegion.RegionType.Continent)
                {
                    var maxIndex = Math.Min(region.Patches.Count, adjacent.Patches.Count);
                    var patchIndex = random.Next(0, maxIndex);
                    LinkPatches(region.Patches[patchIndex], adjacent.Patches[patchIndex]);
                }
            }
        }
    }

    private static void BuildPatches(PatchRegion region, Random random)
    {
        var regionMargin = region.PatchMargin + region.RegionLineWidth;

        // Patch linking first
        if (region.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean or
            PatchRegion.RegionType.Continent)
        {
            for (int i = 0; i < region.Patches.Count - 1; i++)
            {
                if (region.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean)
                {
                    LinkPatches(region.Patches[i], region.Patches[i + 1]);
                }

                if (region.Type == PatchRegion.RegionType.Continent)
                {
                    for (int j = 0; j < region.Patches.Count; j++)
                    {
                        if (j != i)
                        {
                            LinkPatches(region.Patches[i], region.Patches[j]);
                        }
                    }
                }
            }
        }

        if (region.Type == PatchRegion.RegionType.Vent)
        {
            var adjacent = region.Adjacent.First();
            LinkPatches(region.Patches[0], adjacent.Patches[adjacent.Patches.Count - 1]);
        }

        // Patches position configuration
        for (int i = 0; i < region.Patches.Count; i++)
        {
            if (region.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean)
            {
                region.Patches[i].ScreenCoordinates = new Vector2(region.ScreenCoordinates.x + regionMargin,
                    region.ScreenCoordinates.y + i * (64.0f + region.PatchMargin) +
                    region.PatchMargin + region.RegionLineWidth);

                // Random depth for water regions
                if (i == region.Patches.Count - 2)
                {
                    var depth = region.Patches[i].Depth;
                    var seafloor = region.Patches[i + 1];
                    region.Patches[i].Depth[1] = random.Next(depth[0] + 1, depth[1] - 10);

                    seafloor.Depth[0] = region.Patches[i].Depth[1];
                    seafloor.Depth[1] = region.Patches[i].Depth[1] + 10;
                }
            }

            if (region.Type == PatchRegion.RegionType.Continent)
            {
                if (i % 2 == 0)
                {
                    if (i == 0)
                    {
                        region.Patches[i].ScreenCoordinates = new Vector2(region.ScreenCoordinates.x + regionMargin,
                            region.ScreenCoordinates.y + regionMargin);
                    }
                    else
                    {
                        region.Patches[i].ScreenCoordinates = new Vector2(region.ScreenCoordinates.x + regionMargin,
                            region.ScreenCoordinates.y + 64.0f + 2 * region.PatchMargin);
                    }
                }
                else
                {
                    if (i == 1)
                    {
                        region.Patches[i].ScreenCoordinates =
                            new Vector2(region.ScreenCoordinates.x + 2 * region.PatchMargin + 64.0f +
                                region.RegionLineWidth, region.ScreenCoordinates.y + regionMargin);
                    }
                    else
                    {
                        region.Patches[i].ScreenCoordinates =
                            new Vector2(region.ScreenCoordinates.x + 2 * region.PatchMargin + 64.0f +
                                region.RegionLineWidth, region.ScreenCoordinates.y + 2 *
                                region.PatchMargin + 64.0f + region.RegionLineWidth);
                    }
                }
            }

            if (region.Type is PatchRegion.RegionType.Vent or PatchRegion.RegionType.Cave)
            {
                region.Patches[0].ScreenCoordinates = new Vector2(region.ScreenCoordinates.x + regionMargin,
                    region.ScreenCoordinates.y + regionMargin);

                // Caves or vents are the same depth as the adjacent patch
                region.Patches[0].Depth[0] = region.Patches[0].Adjacent.First().Depth[0];
                region.Patches[0].Depth[1] = region.Patches[0].Adjacent.First().Depth[1];
            }
        }
    }

    private static void BuildRegion(PatchRegion region)
    {
        // Region size configuration
        region.Width += 64.0f + 2 * region.PatchMargin + 2 * region.RegionLineWidth;

        if (region.Type == PatchRegion.RegionType.Continent)
        {
            region.Height = 64.0f + 2 * region.PatchMargin + region.RegionLineWidth;
            if (region.Patches.Count > 1)
                region.Width += 64.0f + region.PatchMargin;

            if (region.Patches.Count > 2)
                region.Height = 3 * region.PatchMargin + 2 * 64.0f + region.RegionLineWidth;
        }

        if (region.Type is PatchRegion.RegionType.Ocean or PatchRegion.RegionType.Sea)
        {
            region.Height += 64.0f * region.Patches.Count + (region.Patches.Count + 1) * region.PatchMargin +
                region.RegionLineWidth;
        }

        if (region.Type == PatchRegion.RegionType.Vent)
        {
            region.Height = 64.0f + 2 * region.PatchMargin + region.RegionLineWidth;
            region.Width = 64.0f + 2 * region.PatchMargin + 2 * region.RegionLineWidth;

            var adjacent = region.Adjacent.First();
            region.ScreenCoordinates = adjacent.ScreenCoordinates + new Vector2(0, adjacent.Height) +
                new Vector2(0, 20);
        }

        if (region.Type == PatchRegion.RegionType.Cave)
        {
            region.Height = 64.0f + 2 * region.PatchMargin + region.RegionLineWidth;
            region.Width = 64.0f + 2 * region.PatchMargin + 2 * region.RegionLineWidth;

            var adjacent = region.Adjacent.First();
            var adjacentPatch = region.Patches[0].Adjacent.First();
            if (adjacent.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean)
            {
                region.ScreenCoordinates = adjacent.ScreenCoordinates +
                    new Vector2(adjacent.Width, adjacent.Patches.IndexOf(adjacentPatch) *
                        (64.0f + region.PatchMargin)) + new Vector2(20, 0);
            }

            if (adjacent.Type == PatchRegion.RegionType.Continent)
            {
                var leftSide = adjacent.ScreenCoordinates - new Vector2(region.Width, 0) - new Vector2(20, 0);
                var rightSide = adjacent.ScreenCoordinates + new Vector2(adjacent.Width, 0) + new Vector2(20, 0);
                var leftDist = (adjacentPatch.ScreenCoordinates - leftSide).Length();
                var rightDist = (adjacentPatch.ScreenCoordinates - rightSide).Length();

                if (leftDist < rightDist)
                {
                    region.ScreenCoordinates = leftSide;
                }
                else
                {
                    region.ScreenCoordinates = rightSide;
                }

                region.ScreenCoordinates = new Vector2(region.ScreenCoordinates.x,
                    adjacentPatch.ScreenCoordinates.y - region.PatchMargin - region.RegionLineWidth);
            }
        }

        region.Height += region.RegionLineWidth;
    }

    private static void BuildSpecialRegions(PatchMap map)
    {
        foreach (var region in map.SpecialRegions)
        {
            BuildRegion(region.Value);
        }
    }

    private static void BuildPatchesInRegions(PatchMap map)
    {
        foreach (var region in map.Regions)
        {
            BuildPatches(region.Value, map.Seed);
            foreach (var patch in region.Value.Patches)
            {
                map.AddPatch(patch);
            }
        }
    }

    private static void BuildPatchesInSpecialRegions(PatchMap map)
    {
        foreach (var region in map.SpecialRegions)
        {
            BuildPatches(region.Value, map.Seed);
            foreach (var patch in region.Value.Patches)
            {
                map.AddPatch(patch);
            }
        }
    }

    private static void ConnectPatchesBetweenRegions(PatchMap map)
    {
        foreach (var region in map.Regions)
        {
            ConnectPatchesBetweenRegions(region.Value, map.Seed);
        }
    }

    /// <summary>
    ///   Returns a predefined patch with default values
    /// </summary>
    /// <param name="biome">The requested biome</param>
    /// <param name="id">ID of this patch</param>
    /// <param name="region">Region this patch belongs to</param>
    /// <param name="regionName">Name of the region</param>
    /// <returns>Predefined patch</returns>
    /// <exception cref="InvalidOperationException">Throw if biome is not a valid value</exception>
    private static Patch NewPredefinedPatch(PredefinedBiome biome, int id, PatchRegion region, string regionName)
    {
        var newPatch = biome switch
        {
            PredefinedBiome.Abyssopelagic => new Patch(GetPatchLocalizedName(regionName, "ABYSSOPELAGIC"),
                id, GetBiomeTemplate("abyssopelagic"), region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(300, 400),
            },

            PredefinedBiome.Bathypelagic => new Patch(GetPatchLocalizedName(regionName, "BATHYPELAGIC"),
                id, GetBiomeTemplate("bathypelagic"), region)
            {
                Depth =
                {
                    [0] = 1000,
                    [1] = 4000,
                },
                ScreenCoordinates = new Vector2(200, 300),
            },

            PredefinedBiome.Cave => new Patch(GetPatchLocalizedName(regionName, "UNDERWATERCAVE"),
                id, GetBiomeTemplate("underwater_cave"), region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(300, 200),
            },

            PredefinedBiome.Coastal => new Patch(GetPatchLocalizedName(regionName, "COASTAL"),
                id, GetBiomeTemplate("coastal"), region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(100, 100),
            },

            PredefinedBiome.Epipelagic => new Patch(GetPatchLocalizedName(regionName, "EPIPELAGIC"),
                id, GetBiomeTemplate("default"), region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 100),
            },

            PredefinedBiome.Estuary => new Patch(GetPatchLocalizedName(regionName, "ESTUARY"),
                id, GetBiomeTemplate("estuary"), region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(70, 160),
            },

            PredefinedBiome.IceShelf => new Patch(GetPatchLocalizedName(regionName, "ICESHELF"),
                id, GetBiomeTemplate("ice_shelf"), region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 30),
            },

            PredefinedBiome.Mesopelagic => new Patch(GetPatchLocalizedName(regionName, "MESOPELAGIC"),
                id, GetBiomeTemplate("mesopelagic"), region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(200, 200),
            },

            PredefinedBiome.Seafloor => new Patch(GetPatchLocalizedName(regionName, "SEA_FLOOR"),
                id, GetBiomeTemplate("seafloor"), region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(200, 400),
            },

            PredefinedBiome.Tidepool => new Patch(GetPatchLocalizedName(regionName, "TIDEPOOL"),
                id, GetBiomeTemplate("tidepool"), region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 10,
                },
                ScreenCoordinates = new Vector2(300, 100),
            },

            PredefinedBiome.Vents => new Patch(GetPatchLocalizedName(regionName, "VOLCANIC_VENT"),
                id, GetBiomeTemplate("aavolcanic_vent"), region)
            {
                Depth =
                {
                    [0] = 2500,
                    [1] = 3000,
                },
                ScreenCoordinates = new Vector2(100, 400),
            },
            _ => throw new InvalidOperationException($"{nameof(biome)} is not a valid biome enum."),
        };

        // Add this patch to region
        region.Patches.Add(newPatch);
        return newPatch;
    }

    private static PatchMap PredefinedMap(PatchMap map, string areaName)
    {
        var region = new PatchRegion(0, GetPatchLocalizedName(areaName, string.Empty),
            PatchRegion.RegionType.Predefined, new Vector2(0, 0));

        // Predefined patches
        var coast = NewPredefinedPatch(PredefinedBiome.Coastal, 0, region, areaName);
        var estuary = NewPredefinedPatch(PredefinedBiome.Estuary, 1, region, areaName);
        var tidepool = NewPredefinedPatch(PredefinedBiome.Tidepool, 2, region, areaName);
        var epipelagic = NewPredefinedPatch(PredefinedBiome.Epipelagic, 3, region, areaName);
        var mesopelagic = NewPredefinedPatch(PredefinedBiome.Mesopelagic, 4, region, areaName);
        var bathypelagic = NewPredefinedPatch(PredefinedBiome.Bathypelagic, 5, region, areaName);
        var abyssopelagic = NewPredefinedPatch(PredefinedBiome.Abyssopelagic, 6, region, areaName);
        var seafloor = NewPredefinedPatch(PredefinedBiome.Seafloor, 7, region, areaName);
        var cave = NewPredefinedPatch(PredefinedBiome.Cave, 8, region, areaName);
        var iceShelf = NewPredefinedPatch(PredefinedBiome.IceShelf, 9, region, areaName);
        var vents = NewPredefinedPatch(PredefinedBiome.Vents, 10, region, areaName);

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
        BuildPatchesInRegions(map);
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
