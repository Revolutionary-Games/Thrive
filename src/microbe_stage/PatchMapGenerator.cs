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
        var areaName = nameGenerator.Next(random);

        // Initialize the graphs random parameters
        var regionCoords = new List<Vector2>();
        int [,] graph = new int [100,100];
        int vertexNr = random.Next(4,7);
        int edgeNr = random.Next(vertexNr + 1, 2*vertexNr - 4);
        int minDistance = 200;
        
        // Create the graphs random points
        for (int i = 0;i < vertexNr; i++)
        {
            int x = random.Next(50,770);
            int y = random.Next(50,770);
            var coord = new Vector2(x,y);

            // Check if the region doesnt overlap over other regions
            bool check = CheckPatchDistance(coord, regionCoords, minDistance);
            while (!check)
            {
                x = random.Next(30,770);
                y = random.Next(30,770);
                coord = new Vector2(x,y);
                check = CheckPatchDistance(coord, regionCoords, minDistance);
            }

            regionCoords.Add(coord);

            

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

            var region = new PatchRegion(i, GetPatchLocalizedName(areaName, regionTypeName));
            int numberOfPatches;

            if (regionType == 2)
            {
                numberOfPatches = random.Next(0,4);

                // All continents must have at least 1 coastal patch.
                var patch = predefinedMap.Patches[0];
                region.AddPatch(patch);

                while (numberOfPatches > 0)
                {
                    var patchIndex = random.Next(0,4);
                    region.AddPatch(predefinedMap.Patches[patchIndex]);

                }
                map.AddPatch(patch);
                map.CurrentPatch = patch;

                
            }
            else
            {
                numberOfPatches = random.Next(2,8);
            }


        }

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
    private static bool CheckPatchDistance(Vector2 patchCoord, List<Vector2> allPatchesCoords, int minDistance)
    {
        if (allPatchesCoords.Count == 0)
            return true;

        for (int i = 0;i< allPatchesCoords.Count; i++)
        {
            var dist = (int)patchCoord.DistanceTo(allPatchesCoords[i]);
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

        var vents = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), 3,
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

        var epipelagic = new Patch(GetPatchLocalizedName(areaName, "EPIPELAGIC"), 5,
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

        var bathypelagic = new Patch(GetPatchLocalizedName(areaName, "BATHYPELAGIC"), 6,
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

        var abyssopelagic = new Patch(GetPatchLocalizedName(areaName, "ABYSSOPELAGIC"), 7,
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
