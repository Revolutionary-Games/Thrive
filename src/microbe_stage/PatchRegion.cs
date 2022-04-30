using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;


public class PatchRegion
{
    [JsonProperty]
    public readonly int ID;

    [JsonProperty]
    public readonly ISet<PatchRegion> Adjacent = new HashSet<PatchRegion>();

    [JsonProperty]
    public LocalizedString Name { get; private set; }
    public float PatchNodeWidth = 64.0f;
    public float PatchNodeHeight = 64.0f;
    public string RegionType = null!;

    /// <summary>
    ///   Coordinates this region is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new(0, 0);

    public float PatchMargin = 4f;

    public float Height;

    public float Width;

    public List<Patch> Patches;

    public PatchRegion(int Id, LocalizedString name, string regionType, Vector2 coordinates)
    {
        ID = Id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        RegionType = regionType;
        ScreenCoordinates = coordinates;
    }

    public void AddPatch(Patch patch)
    {
        Patches.Add(patch);
    }

    public void BuildPatches(Random random)
    {

        // Patch linking first
        if ( RegionType == "sea" || RegionType == "ocean" || RegionType == "continent")
        {
            for (int i = 0; i < Patches.Count - 1; i++)
            {
                if (RegionType == "sea" || RegionType == "ocean")
                {
                    LinkPatches(Patches[i], Patches[i+1]);
                    
                }

                if (RegionType == "continent")
                {
                    for (int j = 0; j < Patches.Count; j++)
                    {
                        if (j != i)
                        {
                            LinkPatches(Patches[i], Patches[j]);
                        }
                    }
                }
            }


        }

        if (RegionType == "vents")
        {
            var adjacent = Adjacent.First();
            LinkPatches(Patches[0], adjacent.Patches[adjacent.Patches.Count - 1]);
        }



        // Patches position configuration
        for (int i = 0; i < Patches.Count; i++)
        {
            
            if ( RegionType == "sea" || RegionType == "ocean")
                Patches[i].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + PatchMargin, ScreenCoordinates.y + i * (64f + PatchMargin) + PatchMargin);

            if (RegionType == "continent")
            {
                if (i % 2 == 0)
                {
                    if (i == 0)
                        Patches[i].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + PatchMargin, ScreenCoordinates.y + PatchMargin);
                    else
                        Patches[i].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + PatchMargin, ScreenCoordinates.y + 64f + 2* PatchMargin);
                }
                else
                {
                    if (i == 1)
                        Patches[i].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + 2 * PatchMargin + 64f, ScreenCoordinates.y + PatchMargin);
                    else
                        Patches[i].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + 2 * PatchMargin + 64f, ScreenCoordinates.y + 2* PatchMargin + 64f);
                }   
            }

            if (RegionType == "vents" || RegionType == "underwatercave")
            {
                Patches[0].ScreenCoordinates = new Vector2 (ScreenCoordinates.x + PatchMargin, ScreenCoordinates.y + PatchMargin);
            }
        }


    }
    public void BuildRegion()
    {
        // Region size configuration
        Width += 64f + 2 * PatchMargin;
        
        if (RegionType == "continent")
        {
            Height = 64f + 2*PatchMargin;
            if (Patches.Count > 1)
                Width += 64f + PatchMargin;
            
            if (Patches.Count > 2)
                Height = 3 * PatchMargin + 2 * 64f;
        }

        if (RegionType == "ocean" || RegionType == "sea")
        {
            Height += 64f * Patches.Count + (Patches.Count + 1) * PatchMargin;
        }
        if (RegionType == "vents")
        {
            Height = Width = 64 + 2 * PatchMargin;

            var adjacent = Adjacent.First();
            ScreenCoordinates = adjacent.ScreenCoordinates + new Vector2(0, adjacent.Height) + new Vector2(0, 20);
            
        }
        if (RegionType == "underwatercave")
        {
            Height = Width = 64 + 2 * PatchMargin;

            var adjacent = Adjacent.First();
            var adjacentPatch = Patches[0].Adjacent.First();
            if (adjacent.RegionType == "sea" || adjacent.RegionType == "ocean")
            {
                ScreenCoordinates = adjacent.ScreenCoordinates + new Vector2(adjacent.Width, adjacent.Patches.IndexOf(adjacentPatch) * 
                    (64f + PatchMargin)) + new Vector2 (20, 0);
            }
            if (adjacent.RegionType == "continent")
            {
                var leftSide = adjacent.ScreenCoordinates -  new Vector2(Width, 0) -new Vector2(20, 0);
                var rightSide = adjacent.ScreenCoordinates + new Vector2(adjacent.Width, 0) + new Vector2(20, 0);
                var leftDist = (adjacentPatch.ScreenCoordinates - leftSide).Length();
                var rightDist = (adjacentPatch.ScreenCoordinates - rightSide).Length();
                if (leftDist < rightDist)
                {
                    ScreenCoordinates = leftSide;
                }
                else
                {
                    ScreenCoordinates = rightSide;
                }

                ScreenCoordinates = new Vector2(ScreenCoordinates.x, adjacentPatch.ScreenCoordinates.y - PatchMargin);
            }
            
        }

    }

    public void ConnectPatchesBetweenRegions(Random random)
    {
            if (RegionType == "ocean" || RegionType == "sea")
            {
                foreach (var adjacent in Adjacent)
                {
                    GD.Print(adjacent.RegionType);
                    if (adjacent.RegionType == "continent")
                    {   
                        var patchIndex = random.Next(0, adjacent.Patches.Count - 1);
                        LinkPatches(Patches[0], adjacent.Patches[patchIndex]);
                    }
                    if (adjacent.RegionType == "sea" || adjacent.RegionType == "ocean")
                    {
                        int lowestConnectedLevel = Patches.Count;
                        lowestConnectedLevel = Math.Min(Patches.Count, adjacent.Patches.Count);
                        lowestConnectedLevel = random.Next(0, lowestConnectedLevel - 1);
                        GD.Print(lowestConnectedLevel);
                        for (int i = 0; i <= lowestConnectedLevel; i++)
                            LinkPatches(Patches[i], adjacent.Patches[i]);
                    }
                }
            }

            if (RegionType == "continent")
            {
                foreach (var adjacent in Adjacent)
                    if (adjacent.RegionType == "continent")
                    {
                        var maxIndex = Math.Min(Patches.Count, adjacent.Patches.Count);
                        var patchIndex = random.Next(0, maxIndex);
                        LinkPatches(Patches[patchIndex], adjacent.Patches[patchIndex]);
                    }
            }
    }

    private void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }
    
    /// <summary>
    ///   Adds a connection to patch
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }

    public Vector2 GetSize()
    {
        return new Vector2(Width, Height);
    }

}
