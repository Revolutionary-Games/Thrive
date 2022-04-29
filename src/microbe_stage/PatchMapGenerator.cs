using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        var predefinedMap = new PatchMap();
        predefinedMap = PredefinedMap(predefinedMap, "Pangonia", defaultSpecies);
        random ??= new Random();

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        // Initialize the graphs random parameters
        var regionCoords = new List<Vector2>();
        int [,] graph = new int [100,100];
        int vertexNr = random.Next(6,10);
        int edgeNr = random.Next(vertexNr , 2*vertexNr - 4);
        int minDistance = 20;
        
        var currentPatchId = 0;
        var specialRegionsId = -1;
        // Create the graphs random regions
        for (int i = 0; i < vertexNr; i++)
        {
            var areaName = nameGenerator.Next(random);
            var continentName = nameGenerator.ContinentName;
            var coord = new Vector2(0,0);

            var regionType = random.Next(0,3);
            string regionTypeName; 
            if (regionType == 0)
            {
                regionTypeName = "sea";
            }
            else
            {
                if (regionType == 1)
                {
                    regionTypeName = "ocean";
                }
                else
                {
                    regionTypeName = "continent";
                }
            }

            var region = new PatchRegion(i, GetPatchLocalizedName(continentName, regionTypeName), regionTypeName, coord);
            int numberOfPatches;

            if (regionType == 2)
            {
                numberOfPatches = random.Next(1,4);

                // All continents must have at least 1 coastal patch.
                Patch patch = GetPatchFromPredefinedMap(0, currentPatchId++, predefinedMap, areaName);

                region.AddPatch(patch);


                while (numberOfPatches > 0)
                {
                    var patchIndex = random.Next(0,3);
                    patch = GetPatchFromPredefinedMap(patchIndex, currentPatchId++, predefinedMap, areaName);

                    region.AddPatch(patch);
                    numberOfPatches--;
                }
                
            }
            else
            {
                numberOfPatches = random.Next(0,4);

                // All oceans/seas must have at least 1 epipelagic/ice patch and a seafloor
                Patch patch;
                if (random.Next(0,2) == 1)
                    patch = GetPatchFromPredefinedMap(3, currentPatchId++, predefinedMap, areaName);
                else
                    patch = GetPatchFromPredefinedMap(9, currentPatchId++, predefinedMap, areaName);
                region.AddPatch(patch);

                patch = GetPatchFromPredefinedMap(7, currentPatchId++, predefinedMap, areaName);
                region.AddPatch(patch);

                while (numberOfPatches > 0)
                {
                    var patchIndex = 3 + numberOfPatches;
                    patch = GetPatchFromPredefinedMap(patchIndex, currentPatchId++, predefinedMap, areaName);
                    region.AddPatch(patch);
                    numberOfPatches--;
                }

                // Chance to add a vent region if this region were adding is an ocean one
                if (random.Next(0,2) == 1)
                {
                    var ventRegion = new PatchRegion(specialRegionsId--,  GetPatchLocalizedName(continentName, "vents"), "vents", coord);
                    var ventPatch = GetPatchFromPredefinedMap(10, currentPatchId++, predefinedMap, areaName);
                    ventRegion.AddPatch(ventPatch);
                    map.AddSpecialRegion(ventRegion);
                    LinkRegions(ventRegion, region);
                }
            }
            region.BuildRegion();
            coord = GenerateCoordinates(region, map, random, minDistance);

            // We add the coordinates for the center of the region 
            // since thats the point that will be connected
            regionCoords.Add(coord + region.GetSize()/2f);
            map.AddRegion(region);
        }

        // After building the normal regions we build the special ones and the patches in all regions
        map.BuildSpecialRegions();
        map.BuildPatchesInRegions();

        // We make the graph by substracting edges from its Delaunay Triangulation 
        // as long as the graph stays connected.
        graph = DelaunayTriangulation(graph, regionCoords);
        var currentEdgeNr = CurrentEdgeNumber(graph, vertexNr);

        // Substract edges until we reach the desired edge count.
        while (currentEdgeNr > edgeNr)
        {
            int edgeToDelete = random.Next(1, currentEdgeNr);
            int i = 0;
            int j = 0;
            for (i = 0; i < vertexNr && edgeToDelete != 0; i++)
            {
                for (j = 0; j < vertexNr && edgeToDelete != 0 && j <= i; j++)
                {
                    if (graph[i,j] == 1)
                        edgeToDelete --;
                }
            }
            i--;
            j--;

            // Check if the graph stays connected after substracting the edge
            // otherwise, leave the edge as is.
            graph[i,j] = graph [j,i] = 0;
            if (!CheckConnectivity(graph, vertexNr))
                graph[i,j] = graph [j,i] = 1;
            else
                currentEdgeNr -= 1;
        }

        // Link regions
        for (int k = 0; k < vertexNr; k++)
            for (int l = 0; l < vertexNr; l++)
                if(graph[l,k] == 1)
                    LinkRegions(map.Regions[k], map.Regions[l]);

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

    // Create a triangulation for a certain graph given some vertex coordinates
    private static int [,] DelaunayTriangulation(int [,] graph, List<Vector2>vertexCoords)
    {
        var indices = Geometry.TriangulateDelaunay2d(vertexCoords.ToArray());
        var triangles = indices.ToList<int>();
        for (int i = 0; i < triangles.Count() - 2; i+=3)
        {
            graph[triangles[i] ,triangles[i+1]] = graph[triangles[i+1], triangles[i]] = 1;
            graph[triangles[i+1] ,triangles[i+2]] = graph[triangles[i+2], triangles[i+1]] = 1;
            graph[triangles[i] ,triangles[i+2]] = graph[triangles[i+2], triangles[i]] = 1;
        }
        return graph;
    }

    // DFS graph search
    private static int[] DFS(int [,] graph, int vertexNr, int point, int[] visited)
    {
        visited[point] = 1;
        for (int i = 0; i < vertexNr; i++)
            if (graph[point,i] == 1 && visited[i] == 0)
                visited = DFS(graph, vertexNr, i, visited);
        return visited;
    }
    
    // Checks the graphs connectivity
    private static bool CheckConnectivity(int [,] graph, int vertexNr)
    {
        int [] visited= new int [vertexNr];
        visited = DFS(graph , vertexNr, 0, visited);
        if (visited.Sum() != vertexNr)
            return false;
        return true;
    }

    // Current number of edges in a given graph
    private static int CurrentEdgeNumber(int [,] graph, int vertexNr)
    {
        int edgeNr = 0;
        for (int i = 0; i < vertexNr; i++)
            for (int j = 0; j < vertexNr; j++)
                edgeNr += graph [i,j];
        
        return edgeNr/2;
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
            int x = random.Next(20,900);
            int y = random.Next(20,900);
            var coord = new Vector2(x,y);
            region.ScreenCoordinates = coord;
            
            // Check if the region doesnt overlap over other regions
            bool check = CheckRegionDistance(region, map, minDistance);
            while (!check)
            {
                GD.Print(coord);
                x = random.Next(20,900);
                y = random.Next(20,900);
                coord = new Vector2(x,y);
                region.ScreenCoordinates = coord;
                check = CheckRegionDistance(region, map, minDistance);
            }
            return coord;
    }

    public static Patch GetPatchFromPredefinedMap(int patchId, int newId, PatchMap predefinedMap, string areaName)
    {
        var patch = predefinedMap.Patches[patchId];
        patch = new Patch(GetPatchLocalizedName(areaName, patch.BiomeTemplate.Name), newId++, patch.BiomeTemplate)
        {
            Depth =
            {
                [0] = patch.Depth[0],
                [1] = patch.Depth[1],
            }
        };

        return patch;
    }

    private static PatchMap PredefinedMap(PatchMap map, string areaName, Species defaultSpecies)
    {

        // Predefined patches
        var coast = new Patch(GetPatchLocalizedName(areaName, "COASTAL"), 0,
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

        var estuary = new Patch(GetPatchLocalizedName(areaName, "ESTUARY"), 1,
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

        var tidepool = new Patch(GetPatchLocalizedName(areaName, "TIDEPOOL"), 2,
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

        var epipelagic = new Patch(GetPatchLocalizedName(areaName, "EPIPELAGIC"), 3,
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

        var mesopelagic = new Patch(GetPatchLocalizedName(areaName, "MESOPELAGIC"), 4,
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

        var bathypelagic = new Patch(GetPatchLocalizedName(areaName, "BATHYPELAGIC"), 5,
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

        var abyssopelagic = new Patch(GetPatchLocalizedName(areaName, "ABYSSOPELAGIC"), 6,
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

        var seafloor = new Patch(GetPatchLocalizedName(areaName, "SEA_FLOOR"), 7,
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

        var vents = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), 10,
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
