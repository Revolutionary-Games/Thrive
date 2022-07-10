using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
public static class PatchMapGenerator
{
    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies, Random? random = null)
    {
        random ??= new Random(settings.Seed);

        if (settings.MapType == WorldGenerationSettings.PatchMapType.Classic)
        {
            // Return the classic map if settings ask for it it
            var predefinedMap = GeneratePredefinedMap();

            ConfigureStartingPatch(predefinedMap, settings, defaultSpecies,
                predefinedMap.GetPatch((int)Patch.BiomeTypes.Vents),
                predefinedMap.GetPatch((int)Patch.BiomeTypes.Tidepool), random);
            return predefinedMap;
        }

        var map = new PatchMap();

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        // Initialize the graph's random parameters
        var regionCoordinates = new List<Vector2>();
        int vertexCount = random.Next(6, 10);
        int edgeCount = random.Next(vertexCount, 2 * vertexCount - 4);
        int minDistance = 180;

        int currentPatchId = 0;

        // Potential starting patches, which must be set by the end of the generating process
        Patch? vents = null;
        Patch? tidepool = null;

        // Create the graph's random regions
        for (int i = 0; i < vertexCount; ++i)
        {
            var regionName = nameGenerator.Next(random);
            var continentName = nameGenerator.ContinentName;
            var coordinates = new Vector2(0, 0);

            // We must create regions containing potential starting locations, so do those first
            PatchRegion.RegionType regionType;
            if (vents == null)
            {
                regionType = PatchRegion.RegionType.Sea;
            }
            else if (tidepool == null)
            {
                regionType = PatchRegion.RegionType.Continent;
            }
            else
            {
                regionType = (PatchRegion.RegionType)random.Next(0, 3);
            }

            var region = new PatchRegion(i, GetPatchLocalizedName(continentName, regionType.ToString()),
                regionType, coordinates);
            int numberOfPatches;

            if (regionType == PatchRegion.RegionType.Continent)
            {
                // Ensure the region is non-empty if we need a tidepool
                numberOfPatches = random.Next(tidepool == null ? 1 : 0, 4);

                // All continents must have at least one coastal patch.
                NewPredefinedPatch(Patch.BiomeTypes.Coastal, ++currentPatchId, region, regionName);

                while (numberOfPatches > 0)
                {
                    // Add at least one tidepool to the map, otherwise choose randomly
                    var patchIndex = tidepool == null ? Patch.BiomeTypes.Tidepool : (Patch.BiomeTypes)random.Next(0, 3);
                    var patch = NewPredefinedPatch(patchIndex, ++currentPatchId, region, regionName);
                    --numberOfPatches;

                    if (patchIndex == Patch.BiomeTypes.Tidepool)
                        tidepool = patch;
                }
            }
            else
            {
                numberOfPatches = random.Next(0, 4);

                // All oceans/seas must have at least one epipelagic/ice patch and a seafloor
                NewPredefinedPatch(random.Next(0, 2) == 1 ? Patch.BiomeTypes.Epipelagic : Patch.BiomeTypes.IceShelf,
                    ++currentPatchId, region, regionName);

                // Add the patches between surface and sea floor
                for (int patchIndex = 4; numberOfPatches > 0 && patchIndex <= (int)Patch.BiomeTypes.Abyssopelagic;
                     ++patchIndex, --numberOfPatches)
                {
                    NewPredefinedPatch((Patch.BiomeTypes)patchIndex, ++currentPatchId, region, regionName);
                }

                // Add the seafloor last
                NewPredefinedPatch(Patch.BiomeTypes.Seafloor, ++currentPatchId, region, regionName);

                // Add at least one vent to the map, otherwise chance to add a vent if this is a sea/ocean region
                if (vents == null || random.Next(0, 2) == 1)
                {
                    vents = NewPredefinedPatch(Patch.BiomeTypes.Vents, ++currentPatchId, region, regionName);
                }
            }

            // Random chance to create a cave
            if (random.Next(0, 2) == 1)
            {
                NewPredefinedPatch(Patch.BiomeTypes.Cave, ++currentPatchId, region, regionName);
            }

            BuildRegion(region);
            coordinates = GenerateCoordinates(region, map, random, minDistance);

            // If there is no more place for the current region, abandon it.
            if (coordinates == Vector2.Inf)
            {
                GD.PrintErr("Region abandoned: ", region.ID);
                continue;
            }

            // We add the coordinates for the center of the region
            // since that's the point that will be connected
            regionCoordinates.Add(coordinates + region.Size / 2.0f);
            map.AddRegion(region);
        }

        // After building the normal regions we build the special ones and the patches
        BuildPatchesInRegions(map, random);

        if (vents == null)
            throw new InvalidOperationException("No vent patch created");

        if (tidepool == null)
            throw new InvalidOperationException("No tidepool patch created");

        // This uses random so this affects the edge subtraction, but this doesn't depend on the selected start type
        // so this makes the same map be generated anyway
        ConfigureStartingPatch(map, settings, defaultSpecies, vents, tidepool, random);

        // We make the graph by subtracting edges from its Delaunay Triangulation
        // as long as the graph stays connected.
        // TODO: should the 100 be hardcoded here? Seems like if vertexCount > 100, we'll explode in the below loops
        int[,] graph = DelaunayTriangulation(new int[100, 100], regionCoordinates);
        graph = SubtractEdges(graph, vertexCount, edgeCount, random);

        // Link regions according to the graph matrix
        for (int i = 0; i < vertexCount; ++i)
        {
            for (int k = 0; k < vertexCount; ++k)
            {
                if (graph[k, i] == 1)
                    LinkRegions(map.Regions[i], map.Regions[k]);
            }
        }

        ConnectPatchesBetweenRegions(map, random);
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
            int k;
            for (i = 0, k = 0; i < vertexNumber && edgeToDelete != 0; ++i)
            {
                for (k = 0; k < vertexNumber && edgeToDelete != 0 && k <= i; ++k)
                {
                    if (graph[i, k] == 1)
                        --edgeToDelete;
                }
            }

            --i;
            --k;

            // Check if the graph stays connected after subtracting the edge
            // otherwise, leave the edge as is.
            graph[i, k] = graph[k, i] = 0;

            if (!CheckConnectivity(graph, vertexNumber))
            {
                graph[i, k] = graph[k, i] = 1;
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
    private static int[,] DelaunayTriangulation(int[,] graph, List<Vector2> vertexCoordinates)
    {
        var indices = Geometry.TriangulateDelaunay2d(vertexCoordinates.ToArray());
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
    /// <param name="vertexCount">Count of vertexes</param>
    /// <param name="point">Current point</param>
    /// <param name="visited">Array of already visited vertexes</param>
    private static void DepthFirstGraphTraversal(int[,] graph, int vertexCount, int point, ref int[] visited)
    {
        visited[point] = 1;

        for (var i = 0; i < vertexCount; ++i)
        {
            if (graph[point, i] == 1 && visited[i] == 0)
                DepthFirstGraphTraversal(graph, vertexCount, i, ref visited);
        }
    }

    /// <summary>
    ///   Checks the graph's connectivity
    /// </summary>
    private static bool CheckConnectivity(int[,] graph, int vertexCount)
    {
        int[] visited = new int[vertexCount];
        DepthFirstGraphTraversal(graph, vertexCount, 0, ref visited);
        return visited.Sum() == vertexCount;
    }

    /// <summary>
    ///   Counts the current number of edges in a given graph
    /// </summary>
    private static int CurrentEdgeNumber(int[,] graph, int vertexCount)
    {
        int edgeNumber = 0;
        for (int i = 0; i < vertexCount; ++i)
        {
            for (int k = 0; k < vertexCount; ++k)
            {
                edgeNumber += graph[i, k];
            }
        }

        return edgeNumber / 2;
    }

    /// <summary>
    ///   Checks distance between regions
    /// </summary>
    /// <returns>Returns true if the regions are far away enough (don't overlap)</returns>
    private static bool CheckRegionDistance(PatchRegion region, PatchMap map, int minDistance)
    {
        foreach (var otherRegion in map.Regions.Values)
        {
            if (ReferenceEquals(region, otherRegion))
                continue;

            if (CheckIfRegionsIntersect(region, otherRegion, minDistance))
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
        var coordinates = new Vector2(x, y);
        region.ScreenCoordinates = coordinates;

        // Make sure the region doesn't overlap over other regions
        // Try no more than 100 times to avoid infinite loop.
        bool check = CheckRegionDistance(region, map, minDistance);
        var i = 0;
        while (!check && i < 100)
        {
            x = random.Next(280, 1600);
            y = random.Next(280, 1600);
            coordinates = new Vector2(x, y);
            region.ScreenCoordinates = coordinates;
            check = CheckRegionDistance(region, map, minDistance);

            ++i;
        }

        return check ? coordinates : Vector2.Inf;
    }

    private static void ConnectPatchesBetweenRegions(PatchRegion region, Random random)
    {
        if (region.Type is PatchRegion.RegionType.Ocean or PatchRegion.RegionType.Sea)
        {
            foreach (var adjacent in region.Adjacent)
            {
                if (adjacent.Type == PatchRegion.RegionType.Continent)
                {
                    LinkPatches(region.Patches[0], adjacent.Patches.Random(random));
                }
                else if (adjacent.Type is PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean)
                {
                    var maxIndex = Math.Min(region.Patches.Count, adjacent.Patches.Count) - 1;
                    var lowestConnectedLevel = random.Next(0, maxIndex);

                    for (int i = 0; i <= lowestConnectedLevel; ++i)
                    {
                        LinkPatches(region.Patches[i], adjacent.Patches[i]);
                    }
                }
            }
        }
        else if (region.Type == PatchRegion.RegionType.Continent)
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
        // Basic vectors to simplify later calculation
        var topLeftPatchPosition = region.ScreenCoordinates + new Vector2(
            Constants.PATCH_AND_REGION_MARGIN + 0.5f * Constants.PATCH_REGION_BORDER_WIDTH,
            Constants.PATCH_AND_REGION_MARGIN + 0.5f * Constants.PATCH_REGION_BORDER_WIDTH);
        var offsetHorizontal = new Vector2(Constants.PATCH_NODE_RECT_LENGTH + Constants.PATCH_AND_REGION_MARGIN, 0);
        var offsetVertical = new Vector2(0, Constants.PATCH_NODE_RECT_LENGTH + Constants.PATCH_AND_REGION_MARGIN);

        // Patch linking first
        switch (region.Type)
        {
            case PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean:
            {
                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == Patch.BiomeTypes.Cave);
                var caveLinkedTo = -1;
                var vents = region.Patches.FirstOrDefault(p => p.BiomeType == Patch.BiomeTypes.Vents);

                var waterPatchCount = region.Patches.Count;

                if (cave != null)
                    --waterPatchCount;

                if (vents != null)
                    --waterPatchCount;

                // Cave should be the last patch
                for (var i = 0; i < region.Patches.Count - (cave == null ? 1 : 2); ++i)
                {
                    LinkPatches(region.Patches[i], region.Patches[i + 1]);
                }

                if (cave != null)
                {
                    // Cave shouldn't be linked to seafloor, vent or itself
                    caveLinkedTo = random.Next(0, waterPatchCount - 1);
                    LinkPatches(cave, region.Patches[caveLinkedTo]);
                }

                for (var i = 0; i < waterPatchCount; ++i)
                {
                    region.Patches[i].ScreenCoordinates = topLeftPatchPosition + i * offsetVertical;
                }

                // Random depth for water regions
                var deepestSeaPatch = region.Patches[waterPatchCount - 2];
                var seafloor = region.Patches[waterPatchCount - 1];
                var depth = deepestSeaPatch.Depth;
                deepestSeaPatch.Depth[1] = random.Next(depth[0] + 1, depth[1] - 10);

                seafloor.Depth[0] = deepestSeaPatch.Depth[1];
                seafloor.Depth[1] = deepestSeaPatch.Depth[1] + 10;

                // Build vents and cave position
                if (vents != null || cave != null)
                {
                    var ventOrCaveToTheRight = random.Next(2) == 1;

                    // If the vents and cave is on the left we need to adjust the water patches' position
                    if (!ventOrCaveToTheRight)
                    {
                        for (int i = 0; i < waterPatchCount; i++)
                        {
                            region.Patches[i].ScreenCoordinates += offsetHorizontal;
                        }
                    }

                    if (vents != null)
                    {
                        vents.ScreenCoordinates = region.Patches[waterPatchCount - 1].ScreenCoordinates
                            + (ventOrCaveToTheRight ? 1 : -1) * offsetHorizontal;
                        vents.Depth[0] = region.Patches[waterPatchCount - 1].Depth[0];
                        vents.Depth[1] = region.Patches[waterPatchCount - 1].Depth[1];
                    }

                    if (cave != null)
                    {
                        cave.ScreenCoordinates = region.Patches[caveLinkedTo].ScreenCoordinates
                            + (ventOrCaveToTheRight ? 1 : -1) * offsetHorizontal;
                        cave.Depth[0] = region.Patches[caveLinkedTo].Depth[0];
                        cave.Depth[1] = region.Patches[caveLinkedTo].Depth[1];
                    }
                }

                break;
            }

            case PatchRegion.RegionType.Continent:
            {
                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == Patch.BiomeTypes.Cave);
                var caveLinkedTo = -1;

                var waterPatchCount = region.Patches.Count;

                if (cave != null)
                    --waterPatchCount;

                for (var i = 0; i < waterPatchCount; i++)
                {
                    for (var k = 0; k < waterPatchCount; ++k)
                    {
                        if (k != i)
                        {
                            LinkPatches(region.Patches[i], region.Patches[k]);
                        }
                    }
                }

                if (cave != null)
                {
                    caveLinkedTo = random.Next(0, waterPatchCount);
                    LinkPatches(cave, region.Patches[caveLinkedTo]);
                    cave.Depth[0] = region.Patches[caveLinkedTo].Depth[0];
                    cave.Depth[1] = region.Patches[caveLinkedTo].Depth[1];
                }

                for (var i = 0; i < waterPatchCount; ++i)
                {
                    region.Patches[i].ScreenCoordinates = topLeftPatchPosition + i switch
                    {
                        0 => Vector2.Zero,
                        1 => offsetHorizontal,
                        2 => offsetVertical,
                        3 => offsetHorizontal + offsetVertical,
                        _ => throw new InvalidOperationException("Patch count shouldn't be greater than 4"),
                    };
                }

                if (cave != null)
                {
                    // Adjust water patches' position
                    if (caveLinkedTo is 0 or 2)
                    {
                        for (var i = 0; i < waterPatchCount; i++)
                        {
                            region.Patches[i].ScreenCoordinates += offsetHorizontal;
                        }
                    }

                    cave.ScreenCoordinates = region.Patches[caveLinkedTo].ScreenCoordinates
                        + (caveLinkedTo is 0 or 2 ? -1 : 1) * offsetHorizontal;
                }

                break;
            }
        }
    }

    private static void BuildRegion(PatchRegion region)
    {
        // Initial size with no patch in it
        region.Width = region.Height = Constants.PATCH_REGION_BORDER_WIDTH + Constants.PATCH_AND_REGION_MARGIN;

        // Per patch offset
        const float offset = Constants.PATCH_NODE_RECT_LENGTH + Constants.PATCH_AND_REGION_MARGIN;

        // Region size configuration
        switch (region.Type)
        {
            case PatchRegion.RegionType.Continent:
            {
                region.Width += offset;
                region.Height += offset;

                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == Patch.BiomeTypes.Cave);

                var waterPatchCount = region.Patches.Count;

                if (cave != null)
                {
                    --waterPatchCount;
                    region.Width += offset;
                }

                // Have 2 columns
                if (waterPatchCount > 1)
                    region.Width += offset;

                // Have 2 rows
                if (waterPatchCount > 2)
                    region.Height += offset;

                break;
            }

            case PatchRegion.RegionType.Ocean or PatchRegion.RegionType.Sea:
            {
                var verticalPatchCount = region.Patches.Count(p =>
                    p.BiomeType != Patch.BiomeTypes.Cave && p.BiomeType != Patch.BiomeTypes.Vents);

                region.Width += offset;

                // If a cave or vent is present
                if (verticalPatchCount != region.Patches.Count)
                    region.Width += offset;

                region.Height += verticalPatchCount * offset;

                break;
            }
        }
    }

    private static void BuildPatchesInRegions(PatchMap map, Random random)
    {
        foreach (var region in map.Regions)
        {
            BuildPatches(region.Value, random);
            foreach (var patch in region.Value.Patches)
            {
                map.AddPatch(patch);
            }
        }
    }

    private static void ConnectPatchesBetweenRegions(PatchMap map, Random random)
    {
        foreach (var region in map.Regions)
        {
            ConnectPatchesBetweenRegions(region.Value, random);
        }
    }

    /// <summary>
    ///   Returns a predefined patch with default values.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that after calling this, region and patch has already been linked together.
    ///   </para>
    /// </remarks>
    /// <param name="biome">The requested biome</param>
    /// <param name="id">ID of this patch</param>
    /// <param name="region">Region this patch belongs to</param>
    /// <param name="regionName">Name of the region</param>
    /// <returns>Predefined patch</returns>
    /// <exception cref="InvalidOperationException">Thrown if biome is not a valid value</exception>
    private static Patch NewPredefinedPatch(Patch.BiomeTypes biome, int id, PatchRegion region, string regionName)
    {
        var newPatch = biome switch
        {
            Patch.BiomeTypes.Abyssopelagic => new Patch(GetPatchLocalizedName(regionName, "ABYSSOPELAGIC"),
                id, GetBiomeTemplate("abyssopelagic"), Patch.BiomeTypes.Abyssopelagic, region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(300, 400),
            },

            Patch.BiomeTypes.Bathypelagic => new Patch(GetPatchLocalizedName(regionName, "BATHYPELAGIC"),
                id, GetBiomeTemplate("bathypelagic"), Patch.BiomeTypes.Bathypelagic, region)
            {
                Depth =
                {
                    [0] = 1000,
                    [1] = 4000,
                },
                ScreenCoordinates = new Vector2(200, 300),
            },

            Patch.BiomeTypes.Cave => new Patch(GetPatchLocalizedName(regionName, "UNDERWATERCAVE"),
                id, GetBiomeTemplate("underwater_cave"), Patch.BiomeTypes.Cave, region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(300, 200),
            },

            Patch.BiomeTypes.Coastal => new Patch(GetPatchLocalizedName(regionName, "COASTAL"),
                id, GetBiomeTemplate("coastal"), Patch.BiomeTypes.Coastal, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(100, 100),
            },

            Patch.BiomeTypes.Epipelagic => new Patch(GetPatchLocalizedName(regionName, "EPIPELAGIC"),
                id, GetBiomeTemplate("default"), Patch.BiomeTypes.Epipelagic, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 100),
            },

            Patch.BiomeTypes.Estuary => new Patch(GetPatchLocalizedName(regionName, "ESTUARY"),
                id, GetBiomeTemplate("estuary"), Patch.BiomeTypes.Estuary, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(70, 160),
            },

            Patch.BiomeTypes.IceShelf => new Patch(GetPatchLocalizedName(regionName, "ICESHELF"),
                id, GetBiomeTemplate("ice_shelf"), Patch.BiomeTypes.IceShelf, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 30),
            },

            Patch.BiomeTypes.Mesopelagic => new Patch(GetPatchLocalizedName(regionName, "MESOPELAGIC"),
                id, GetBiomeTemplate("mesopelagic"), Patch.BiomeTypes.Mesopelagic, region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(200, 200),
            },

            Patch.BiomeTypes.Seafloor => new Patch(GetPatchLocalizedName(regionName, "SEA_FLOOR"),
                id, GetBiomeTemplate("seafloor"), Patch.BiomeTypes.Seafloor, region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(200, 400),
            },

            Patch.BiomeTypes.Tidepool => new Patch(GetPatchLocalizedName(regionName, "TIDEPOOL"),
                id, GetBiomeTemplate("tidepool"), Patch.BiomeTypes.Tidepool, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 10,
                },
                ScreenCoordinates = new Vector2(300, 100),
            },

            // ReSharper disable once StringLiteralTypo
            Patch.BiomeTypes.Vents => new Patch(GetPatchLocalizedName(regionName, "VOLCANIC_VENT"),
                id, GetBiomeTemplate("aavolcanic_vent"), Patch.BiomeTypes.Vents, region)
            {
                Depth =
                {
                    [0] = 2500,
                    [1] = 3000,
                },
                ScreenCoordinates = new Vector2(100, 400),
            },
            _ => throw new InvalidOperationException($"{nameof(biome)} is not a valid biome enum value."),
        };

        // Add this patch to the region
        region.Patches.Add(newPatch);
        return newPatch;
    }

    private static PatchMap GeneratePredefinedMap()
    {
        var map = new PatchMap();
        var areaName = TranslationServer.Translate("PANGONIAN_REGION_NAME");

        var region = new PatchRegion(0, GetPatchLocalizedName(areaName, string.Empty),
            PatchRegion.RegionType.Predefined, new Vector2(0, 0));

        // Hard code the region size as slightly larger than the extreme patch edges to fix scrolling
        region.Size = new Vector2(400, 500);

        // Predefined patches
        var coast = NewPredefinedPatch(Patch.BiomeTypes.Coastal, 0, region, areaName);
        var estuary = NewPredefinedPatch(Patch.BiomeTypes.Estuary, 1, region, areaName);
        var tidepool = NewPredefinedPatch(Patch.BiomeTypes.Tidepool, 2, region, areaName);
        var epipelagic = NewPredefinedPatch(Patch.BiomeTypes.Epipelagic, 3, region, areaName);
        var mesopelagic = NewPredefinedPatch(Patch.BiomeTypes.Mesopelagic, 4, region, areaName);
        var bathypelagic = NewPredefinedPatch(Patch.BiomeTypes.Bathypelagic, 5, region, areaName);
        var abyssopelagic = NewPredefinedPatch(Patch.BiomeTypes.Abyssopelagic, 6, region, areaName);
        var seafloor = NewPredefinedPatch(Patch.BiomeTypes.Seafloor, 7, region, areaName);
        var cave = NewPredefinedPatch(Patch.BiomeTypes.Cave, 8, region, areaName);
        var iceShelf = NewPredefinedPatch(Patch.BiomeTypes.IceShelf, 9, region, areaName);
        var vents = NewPredefinedPatch(Patch.BiomeTypes.Vents, 10, region, areaName);

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

        foreach (var patch in region.Patches)
            map.AddPatch(patch);

        return map;
    }

    private static void ConfigureStartingPatch(PatchMap map, WorldGenerationSettings settings, Species defaultSpecies,
        Patch vents, Patch tidepool, Random random)
    {
        // Choose this here to ensure the same seed creates the same world regardless of starting location type
        var randomPatch = map.Patches.Random(random) ?? throw new Exception("No patches to pick from");

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
