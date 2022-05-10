using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A region is a somehting like a continent/ocean that contains multiple biomes(patches).
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class PatchRegion
{
    [JsonProperty]
    public readonly int ID;

    [JsonProperty]
    public readonly ISet<PatchRegion> Adjacent = new HashSet<PatchRegion>();

    [JsonProperty]
    public float PatchNodeWidth = 64.0f;

    [JsonProperty]
    public float PatchNodeHeight = 64.0f;

    [JsonProperty]
    public float RegionLineWidth = 4f;

    [JsonProperty]
    public string RegionType;

    [JsonProperty]
    public float PatchMargin = 4f;

    [JsonProperty]
    public float Height;

    [JsonProperty]
    public float Width;

    [JsonProperty]
    public List<Patch> Patches;

    public PatchRegion(int id, LocalizedString name, string regionType, Vector2 coordinates)
    {
        ID = id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        RegionType = regionType;
        ScreenCoordinates = coordinates;
    }

    [JsonProperty]
    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; }

    public void BuildPatches(Random random)
    {
        var regionMargin = PatchMargin + RegionLineWidth;

        // Patch linking first
        if (RegionType is "sea" or "ocean" or "continent")
        {
            for (int i = 0; i < Patches.Count - 1; i++)
            {
                if (RegionType is "sea" or "ocean")
                {
                    LinkPatches(Patches[i], Patches[i + 1]);
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
            if (RegionType is "sea" or "ocean")
            {
                Patches[i].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                    ScreenCoordinates.y + i * (64f + PatchMargin) + PatchMargin + RegionLineWidth);
            }

            if (RegionType == "continent")
            {
                if (i % 2 == 0)
                {
                    if (i == 0)
                    {
                        Patches[i].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                            ScreenCoordinates.y + regionMargin);
                    }
                    else
                    {
                        Patches[i].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                            ScreenCoordinates.y + 64f + 2 * PatchMargin);
                    }
                }
                else
                {
                    if (i == 1)
                    {
                        Patches[i].ScreenCoordinates =
                            new Vector2(ScreenCoordinates.x + 2 * PatchMargin + 64f + RegionLineWidth,
                                ScreenCoordinates.y + regionMargin);
                    }
                    else
                    {
                        Patches[i].ScreenCoordinates =
                            new Vector2(ScreenCoordinates.x + 2 * PatchMargin + 64f + RegionLineWidth,
                                ScreenCoordinates.y + 2 * PatchMargin + 64f + RegionLineWidth);
                    }
                }
            }

            if (RegionType is "vents" or "underwatercave")
            {
                Patches[0].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                    ScreenCoordinates.y + regionMargin);
            }
        }
    }

    public void BuildRegion()
    {
        // Region size configuration
        Width += 64f + 2 * PatchMargin + RegionLineWidth;

        if (RegionType == "continent")
        {
            Height = 64f + 2 * PatchMargin + RegionLineWidth;
            if (Patches.Count > 1)
                Width += 64f + PatchMargin;

            if (Patches.Count > 2)
                Height = 3 * PatchMargin + 2 * 64f + RegionLineWidth;
        }

        if (RegionType is "ocean" or "sea")
        {
            Height += 64f * Patches.Count + (Patches.Count + 1) * PatchMargin + RegionLineWidth;
        }

        if (RegionType == "vents")
        {
            Height = Width = 64 + 2 * PatchMargin + RegionLineWidth;

            var adjacent = Adjacent.First();
            ScreenCoordinates = adjacent.ScreenCoordinates + new Vector2(0, adjacent.Height) + new Vector2(0, 20);
        }

        if (RegionType == "underwatercave")
        {
            Height = Width = 64 + 2 * PatchMargin + RegionLineWidth;

            var adjacent = Adjacent.First();
            var adjacentPatch = Patches[0].Adjacent.First();
            if (adjacent.RegionType is "sea" or "ocean")
            {
                ScreenCoordinates = adjacent.ScreenCoordinates +
                    new Vector2(adjacent.Width, adjacent.Patches.IndexOf(adjacentPatch) *
                        (64f + PatchMargin)) + new Vector2(20, 0);
            }

            if (adjacent.RegionType == "continent")
            {
                var leftSide = adjacent.ScreenCoordinates - new Vector2(Width, 0) - new Vector2(20, 0);
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

                ScreenCoordinates = new Vector2(ScreenCoordinates.x,
                    adjacentPatch.ScreenCoordinates.y - PatchMargin - RegionLineWidth);
            }
        }
    }

    public void ConnectPatchesBetweenRegions(Random random)
    {
        if (RegionType is "ocean" or "sea")
        {
            foreach (var adjacent in Adjacent)
            {
                if (adjacent.RegionType == "continent")
                {
                    var patchIndex = random.Next(0, adjacent.Patches.Count - 1);
                    LinkPatches(Patches[0], adjacent.Patches[patchIndex]);
                }

                if (adjacent.RegionType is "sea" or "ocean")
                {
                    int lowestConnectedLevel;
                    lowestConnectedLevel = Math.Min(Patches.Count, adjacent.Patches.Count);
                    lowestConnectedLevel = random.Next(0, lowestConnectedLevel - 1);

                    for (int i = 0; i <= lowestConnectedLevel; i++)
                    {
                        LinkPatches(Patches[i], adjacent.Patches[i]);
                    }
                }
            }
        }

        if (RegionType == "continent")
        {
            foreach (var adjacent in Adjacent)
            {
                if (adjacent.RegionType == "continent")
                {
                    var maxIndex = Math.Min(Patches.Count, adjacent.Patches.Count);
                    var patchIndex = random.Next(0, maxIndex);
                    LinkPatches(Patches[patchIndex], adjacent.Patches[patchIndex]);
                }
            }
        }
    }

    public void AddPatch(Patch patch)
    {
        Patches.Add(patch);
        patch.Region = this;
    }

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }

    /// <summary>
    ///   Returns the regions size
    /// </summary>
    public Vector2 GetSize()
    {
        return new Vector2(Width, Height);
    }

    private void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }
}
