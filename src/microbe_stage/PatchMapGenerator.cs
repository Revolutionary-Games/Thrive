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

        var map = new PatchMap();

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        // Initialize the graph's random parameters
        var regionCoordinates = new List<Vector2>();
        int vertexCount = random.Next(6, 10);
        int minDistance = 180;

        int currentPatchId = 0;

        // Potential starting patches, which must be set by the end of the generating process
        Patch? vents = null;
        Patch? tidepool = null;

        // Create the graph's random regions
        // i is used as region id. They need to start at 0 for the hardcoded triangulation algorithm below to work
        for (int i = 0; i < vertexCount; ++i)
        {
            var (continentName, regionName) = nameGenerator.Next(random);
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

            var region = new PatchRegion(i, continentName, regionType, coordinates);
            int numberOfPatches;

            if (regionType == PatchRegion.RegionType.Continent)
            {
                // All continents must have at least one coastal patch.
                NewPredefinedPatch(BiomeType.Coastal, ++currentPatchId, region, regionName);

                // Ensure the region is non-empty if we need a tidepool.
                // Region should not have duplicate biomes, so at most 2 patches will be added.
                numberOfPatches = random.Next(tidepool == null ? 1 : 0, 3);

                using var availableBiomes =
                    new[] { BiomeType.Estuary, BiomeType.Tidepool }.OrderBy(_ => random.Next()).GetEnumerator();

                while (numberOfPatches > 0)
                {
                    // Add at least one tidepool to the map, otherwise choose randomly
                    availableBiomes.MoveNext();
                    var biomeType = availableBiomes.Current;
                    var patch = NewPredefinedPatch(biomeType, ++currentPatchId, region, regionName);
                    --numberOfPatches;

                    if (biomeType == BiomeType.Tidepool)
                        tidepool ??= patch;
                }

                // If there's no tidepool, add one
                tidepool ??= NewPredefinedPatch(BiomeType.Tidepool, ++currentPatchId, region, regionName);
            }
            else
            {
                numberOfPatches = random.Next(0, 4);

                // All oceans/seas must have at least one epipelagic/ice patch and a seafloor
                NewPredefinedPatch(random.Next(0, 2) == 1 ? BiomeType.Epipelagic : BiomeType.IceShelf,
                    ++currentPatchId, region, regionName);

                // Add the patches between surface and sea floor
                for (int patchIndex = 4; numberOfPatches > 0 && patchIndex <= (int)BiomeType.Abyssopelagic;
                     ++patchIndex, --numberOfPatches)
                {
                    NewPredefinedPatch((BiomeType)patchIndex, ++currentPatchId, region, regionName);
                }

                // Add the seafloor last
                NewPredefinedPatch(BiomeType.Seafloor, ++currentPatchId, region, regionName);

                // Add at least one vent to the map, otherwise chance to add a vent if this is a sea/ocean region
                if (vents == null || random.Next(0, 2) == 1)
                {
                    // First call the function to add the vents to the region
                    var patch = NewPredefinedPatch(BiomeType.Vents, ++currentPatchId, region, regionName);

                    // Then update vents variable if null
                    vents ??= patch;
                }
            }

            // Random chance to create a cave
            if (random.Next(0, 2) == 1)
            {
                NewPredefinedPatch(BiomeType.Cave, ++currentPatchId, region, regionName);
            }

            BuildRegionSize(region);
            coordinates = GenerateCoordinates(region, map, random, minDistance);

            // If there is no more place for the current region, abandon it.
            // TODO: modify the algorithm to not need to abandon regions
            if (coordinates == Vector2.Inf)
            {
                GD.PrintErr("Region abandoned: ", region.ID);
                vertexCount = i;
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
            throw new InvalidOperationException($"No vent patch created for seed {settings.Seed}");

        if (tidepool == null)
            throw new InvalidOperationException($"No tidepool patch created for seed {settings.Seed}");

        // This uses random so this affects the edge subtraction, but this doesn't depend on the selected start type
        // so this makes the same map be generated anyway
        ConfigureStartingPatch(map, settings, defaultSpecies, vents, tidepool, random);

        var edgeCount = random.Next(vertexCount, 2 * vertexCount - 4);

        // We make the graph by subtracting edges from its Delaunay Triangulation
        // as long as the graph stays connected.
        var graph = new bool[vertexCount, vertexCount];
        DelaunayTriangulation(ref graph, regionCoordinates);
        SubtractEdges(ref graph, vertexCount, edgeCount, random);

        // Link regions according to the graph matrix
        for (int i = 0; i < vertexCount; ++i)
        {
            for (int k = 0; k < vertexCount; ++k)
            {
                if (graph[k, i])
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

        var region1 = patch1.Region;
        var region2 = patch2.Region;

        if (region1 != region2)
        {
            region1.SetConnectingPatch(region2, patch2);
            region2.SetConnectingPatch(region1, patch1);
        }
    }

    private static void LinkRegions(PatchRegion region1, PatchRegion region2)
    {
        region1.AddNeighbour(region2);
        region2.AddNeighbour(region1);
    }

    /// <summary>
    ///   Creates a triangulation for a certain graph given some vertex coordinates
    /// </summary>
    private static void DelaunayTriangulation(ref bool[,] graph, List<Vector2> vertexCoordinates)
    {
        var triangles = Geometry.TriangulateDelaunay2d(vertexCoordinates.ToArray());
        for (var i = 0; i < triangles.Length; i += 3)
        {
            graph[triangles[i], triangles[i + 1]] = graph[triangles[i + 1], triangles[i]] = true;
            graph[triangles[i + 1], triangles[i + 2]] = graph[triangles[i + 2], triangles[i + 1]] = true;
            graph[triangles[i], triangles[i + 2]] = graph[triangles[i + 2], triangles[i]] = true;
        }
    }

    private static void SubtractEdges(ref bool[,] graph, int vertexCount, int edgeCount, Random random)
    {
        var currentEdgeCount = CurrentEdgeNumber(ref graph, vertexCount);

        // Subtract edges until we reach the desired edge count.
        while (currentEdgeCount > edgeCount)
        {
            int edgeToDelete = random.Next(1, currentEdgeCount + 1);
            int i;
            int k;

            for (i = 0, k = 0; i < vertexCount && edgeToDelete != 0; ++i)
            {
                for (k = 0; edgeToDelete != 0 && k < i; ++k)
                {
                    if (graph[i, k])
                        --edgeToDelete;
                }
            }

            // Compensate for the ++i, ++k at the end of the loop
            --i;
            --k;

            // Check if the graph stays connected after subtracting the edge
            // otherwise, leave the edge as is.
            graph[i, k] = graph[k, i] = false;

            if (!CheckConnectivity(ref graph, vertexCount))
            {
                graph[i, k] = graph[k, i] = true;
            }
            else
            {
                currentEdgeCount -= 1;
            }
        }
    }

    /// <summary>
    ///   DFS graph traversal to get all connected nodes
    /// </summary>
    /// <param name="graph">The graph to test</param>
    /// <param name="vertexCount">Count of vertexes</param>
    /// <param name="point">Current point</param>
    /// <param name="visited">
    ///   Array of already visited vertexes, each boolean marks whether a vertex at that index is visited or not
    /// </param>
    private static void DepthFirstGraphTraversal(ref bool[,] graph, int vertexCount, int point, ref bool[] visited)
    {
        visited[point] = true;

        for (int i = 0; i < vertexCount; ++i)
        {
            if (graph[point, i] && !visited[i])
                DepthFirstGraphTraversal(ref graph, vertexCount, i, ref visited);
        }
    }

    /// <summary>
    ///   Checks the graph's connectivity
    /// </summary>
    private static bool CheckConnectivity(ref bool[,] graph, int vertexCount)
    {
        bool[] visited = new bool[vertexCount];
        DepthFirstGraphTraversal(ref graph, vertexCount, 0, ref visited);
        return visited.Count(v => v) == vertexCount;
    }

    /// <summary>
    ///   Counts the current number of edges in a given graph
    /// </summary>
    private static int CurrentEdgeNumber(ref bool[,] graph, int vertexCount)
    {
        int edgeNumber = 0;
        for (int i = 0; i < vertexCount; ++i)
        {
            for (int k = 0; k < i; ++k)
            {
                if (graph[i, k])
                    ++edgeNumber;
            }
        }

        return edgeNumber;
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
        Vector2 coordinate;

        int i = 0;

        // Make sure the region doesn't overlap over other regions
        // Try no more than Constants.PATCH_GENERATION_MAX_RETRIES times to avoid infinite loop.
        do
        {
            coordinate = new Vector2(random.Next(3, 16) * 100, random.Next(3, 16) * 100);
            region.ScreenCoordinates = coordinate;
        }
        while (!CheckRegionDistance(region, map, minDistance) && ++i < Constants.PATCH_GENERATION_MAX_RETRIES);

        return i < Constants.PATCH_GENERATION_MAX_RETRIES ? coordinate : Vector2.Inf;
    }

    private static bool IsWaterPatch(Patch patch)
    {
        return patch.BiomeType != BiomeType.Cave && patch.BiomeType != BiomeType.Vents;
    }

    private static void BuildPatchesInRegion(PatchRegion region, Random random)
    {
        var sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

        // For now minimum sunlight is always 0
        foreach (var regionPatch in region.Patches)
        {
            regionPatch.Biome.MinimumCompounds[sunlightCompound] = new BiomeCompoundProperties
            {
                Ambient = 0,
            };
        }

        // Base vectors to simplify later calculation
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
                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == BiomeType.Cave);
                int caveLinkedTo = -1;
                var vents = region.Patches.FirstOrDefault(p => p.BiomeType == BiomeType.Vents);

                int waterPatchCount = region.Patches.Count;

                if (cave != null)
                    --waterPatchCount;

                if (vents != null)
                    --waterPatchCount;

                // Cave should be the last patch
                for (int i = 0; i < region.Patches.Count - (cave == null ? 1 : 2); ++i)
                {
                    LinkPatches(region.Patches[i], region.Patches[i + 1]);
                }

                if (cave != null)
                {
                    // Cave shouldn't be linked to seafloor, vent or itself
                    caveLinkedTo = random.Next(0, waterPatchCount - 1);
                    LinkPatches(cave, region.Patches[caveLinkedTo]);
                }

                for (int i = 0; i < waterPatchCount; ++i)
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

                // Build seafloor light, using 0m -> 1, 200m -> 0.01, floor to 0.01
                var sunlightProperty = seafloor.Biome.ChangeableCompounds[sunlightCompound];
                var sunlightAmount = (int)(Mathf.Pow(0.977237220956f, seafloor.Depth[1]) * 100) / 100.0f;
                sunlightProperty.Ambient = sunlightAmount;

                seafloor.Biome.ChangeableCompounds[sunlightCompound] = sunlightProperty;

                // Build vents and cave position
                if (vents != null || cave != null)
                {
                    bool ventOrCaveToTheRight = random.Next(2) == 1;

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
                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == BiomeType.Cave);
                int caveLinkedTo = -1;

                int waterPatchCount = region.Patches.Count;

                if (cave != null)
                    --waterPatchCount;

                for (int i = 0; i < waterPatchCount; ++i)
                {
                    for (int k = 0; k < waterPatchCount; ++k)
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

                for (int i = 0; i < waterPatchCount; ++i)
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
                        for (int i = 0; i < waterPatchCount; i++)
                        {
                            region.Patches[i].ScreenCoordinates += offsetHorizontal;
                        }
                    }

                    cave.ScreenCoordinates = region.Patches[caveLinkedTo].ScreenCoordinates
                        + offsetHorizontal * (caveLinkedTo is 0 or 2 ? -1 : 1);
                }

                break;
            }
        }
    }

    private static void BuildRegionSize(PatchRegion region)
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

                var cave = region.Patches.FirstOrDefault(p => p.BiomeType == BiomeType.Cave);

                int waterPatchCount = region.Patches.Count;

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
                int verticalPatchCount = region.Patches.Count(IsWaterPatch);

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
            BuildPatchesInRegion(region.Value, random);
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

    private static void ConnectPatchesBetweenRegions(PatchRegion region, Random random)
    {
        switch (region.Type)
        {
            case PatchRegion.RegionType.Ocean or PatchRegion.RegionType.Sea:
            {
                foreach (var adjacent in region.Adjacent.CloneShallow().Keys)
                {
                    switch (adjacent.Type)
                    {
                        case PatchRegion.RegionType.Sea or PatchRegion.RegionType.Ocean:
                        {
                            int maxIndex =
                                Math.Min(region.Patches.Count(IsWaterPatch), adjacent.Patches.Count(IsWaterPatch)) - 1;

                            int lowestConnectedLevel = random.Next(0, maxIndex);

                            for (int i = 0; i <= lowestConnectedLevel; ++i)
                            {
                                LinkPatches(region.Patches[i], adjacent.Patches[i]);
                            }

                            break;
                        }

                        case PatchRegion.RegionType.Continent:
                        {
                            LinkPatches(region.Patches[0], adjacent.Patches.OrderBy(_ => random.Next())
                                .First(p => IsWaterPatch(p) && p.BiomeType != BiomeType.Tidepool));
                            break;
                        }
                    }
                }

                break;
            }

            case PatchRegion.RegionType.Continent:
            {
                foreach (var adjacent in region.Adjacent.CloneShallow().Keys)
                {
                    if (adjacent.Type == PatchRegion.RegionType.Continent)
                    {
                        int maxIndex =
                            Math.Min(region.Patches.Count(IsWaterPatch), adjacent.Patches.Count(IsWaterPatch));

                        int patchIndex = random.Next(0, maxIndex);
                        LinkPatches(region.Patches[patchIndex], adjacent.Patches[patchIndex]);
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    ///   Returns a predefined patch with default values.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that after calling this, regions and patches have already been linked together.
    ///   </para>
    /// </remarks>
    /// <param name="biome">The requested biome</param>
    /// <param name="id">ID of this patch</param>
    /// <param name="region">Region this patch belongs to</param>
    /// <param name="regionName">Name of the region</param>
    /// <returns>Predefined patch</returns>
    /// <exception cref="InvalidOperationException">Thrown if biome is not a valid value</exception>
    private static Patch NewPredefinedPatch(BiomeType biome, int id, PatchRegion region, string regionName)
    {
        var newPatch = biome switch
        {
            BiomeType.Abyssopelagic => new Patch(GetPatchLocalizedName(regionName, "ABYSSOPELAGIC"),
                id, GetBiomeTemplate("abyssopelagic"), BiomeType.Abyssopelagic, region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(300, 400),
            },

            BiomeType.Bathypelagic => new Patch(GetPatchLocalizedName(regionName, "BATHYPELAGIC"),
                id, GetBiomeTemplate("bathypelagic"), BiomeType.Bathypelagic, region)
            {
                Depth =
                {
                    [0] = 1000,
                    [1] = 4000,
                },
                ScreenCoordinates = new Vector2(200, 300),
            },

            BiomeType.Cave => new Patch(GetPatchLocalizedName(regionName, "UNDERWATERCAVE"),
                id, GetBiomeTemplate("underwater_cave"), BiomeType.Cave, region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(300, 200),
            },

            BiomeType.Coastal => new Patch(GetPatchLocalizedName(regionName, "COASTAL"),
                id, GetBiomeTemplate("coastal"), BiomeType.Coastal, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(100, 100),
            },

            BiomeType.Epipelagic => new Patch(GetPatchLocalizedName(regionName, "EPIPELAGIC"),
                id, GetBiomeTemplate("default"), BiomeType.Epipelagic, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 100),
            },

            BiomeType.Estuary => new Patch(GetPatchLocalizedName(regionName, "ESTUARY"),
                id, GetBiomeTemplate("estuary"), BiomeType.Estuary, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(70, 160),
            },

            BiomeType.IceShelf => new Patch(GetPatchLocalizedName(regionName, "ICESHELF"),
                id, GetBiomeTemplate("ice_shelf"), BiomeType.IceShelf, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 200,
                },
                ScreenCoordinates = new Vector2(200, 30),
            },

            BiomeType.Mesopelagic => new Patch(GetPatchLocalizedName(regionName, "MESOPELAGIC"),
                id, GetBiomeTemplate("mesopelagic"), BiomeType.Mesopelagic, region)
            {
                Depth =
                {
                    [0] = 200,
                    [1] = 1000,
                },
                ScreenCoordinates = new Vector2(200, 200),
            },

            BiomeType.Seafloor => new Patch(GetPatchLocalizedName(regionName, "SEA_FLOOR"),
                id, GetBiomeTemplate("seafloor"), BiomeType.Seafloor, region)
            {
                Depth =
                {
                    [0] = 4000,
                    [1] = 6000,
                },
                ScreenCoordinates = new Vector2(200, 400),
            },

            BiomeType.Tidepool => new Patch(GetPatchLocalizedName(regionName, "TIDEPOOL"),
                id, GetBiomeTemplate("tidepool"), BiomeType.Tidepool, region)
            {
                Depth =
                {
                    [0] = 0,
                    [1] = 10,
                },
                ScreenCoordinates = new Vector2(300, 100),
            },

            // ReSharper disable once StringLiteralTypo
            BiomeType.Vents => new Patch(GetPatchLocalizedName(regionName, "VOLCANIC_VENT"),
                id, GetBiomeTemplate("aavolcanic_vent"), BiomeType.Vents, region)
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

        map.CurrentPatch.AddSpecies(defaultSpecies, Constants.INITIAL_SPECIES_POPULATION);
    }

    private static LocalizedString GetPatchLocalizedName(string regionName, string biomeType)
    {
        return new LocalizedString("PATCH_NAME", regionName, new LocalizedString(biomeType));
    }
}
