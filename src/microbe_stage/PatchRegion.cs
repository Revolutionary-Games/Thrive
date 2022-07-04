﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A region is a something like a continent/ocean that contains multiple patches.
/// </summary>
[UseThriveSerializer]
public class PatchRegion
{
    // TODO: Move these to Constants.cs

    [JsonIgnore]
    public float PatchNodeWidth = 64.0f;

    [JsonIgnore]
    public float PatchNodeHeight = 64.0f;

    [JsonIgnore]
    public float RegionLineWidth = 4.0f;

    [JsonIgnore]
    public float PatchMargin = 4.0f;

    public enum RegionType
    {
        Predefined,
        Sea,
        Ocean,
        Continent,
        Vent,
        Cave,
    }

    [JsonConstructor]
    public PatchRegion(int id, LocalizedString name, RegionType regionType, Vector2 screenCoordinates,
        float height, float width, List<Patch> patches)
    {
        ID = id;
        Name = name;
        Type = regionType;
        Patches = patches;
        ScreenCoordinates = screenCoordinates;
        Height = height;
        Width = width;
    }

    public PatchRegion(int id, LocalizedString name, RegionType regionType, Vector2 screenCoordinates)
    {
        ID = id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        Type = regionType;
        ScreenCoordinates = screenCoordinates;
    }

    [JsonProperty]
    public int ID { get; }

    [JsonIgnore]
    public ISet<PatchRegion> Adjacent { get; } = new HashSet<PatchRegion>();

    [JsonProperty]
    public RegionType Type { get; set; }

    [JsonProperty]
    public float Height { get; set; }

    [JsonProperty]
    public float Width { get; set; }

    [JsonIgnore]
    public Vector2 Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.x;
            Height = value.y;
        }
    }

    [JsonProperty]
    public List<Patch> Patches { get; set; }

    [JsonProperty]
    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed in the GUI
    /// </summary>
    [JsonProperty]
    public Vector2 ScreenCoordinates { get; set; }

    public void BuildPatches(Random random)
    {
        var regionMargin = PatchMargin + RegionLineWidth;

        // Patch linking first
        if (Type is RegionType.Sea or RegionType.Ocean or RegionType.Continent)
        {
            for (int i = 0; i < Patches.Count - 1; i++)
            {
                if (Type is RegionType.Sea or RegionType.Ocean)
                {
                    LinkPatches(Patches[i], Patches[i + 1]);
                }

                if (Type == RegionType.Continent)
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

        if (Type == RegionType.Vent)
        {
            var adjacent = Adjacent.First();
            LinkPatches(Patches[0], adjacent.Patches[adjacent.Patches.Count - 1]);
        }

        // Patches position configuration
        for (int i = 0; i < Patches.Count; i++)
        {
            if (Type is RegionType.Sea or RegionType.Ocean)
            {
                Patches[i].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                    ScreenCoordinates.y + i * (64.0f + PatchMargin) + PatchMargin + RegionLineWidth);

                // Random depth for water regions
                if (i == Patches.Count - 2)
                {
                    var depth = Patches[i].Depth;
                    var seafloor = Patches[i + 1];
                    Patches[i].Depth[1] = random.Next(depth[0] + 1, depth[1] - 10);

                    seafloor.Depth[0] = Patches[i].Depth[1];
                    seafloor.Depth[1] = Patches[i].Depth[1] + 10;
                }
            }

            if (Type == RegionType.Continent)
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
                            ScreenCoordinates.y + 64.0f + 2 * PatchMargin);
                    }
                }
                else
                {
                    if (i == 1)
                    {
                        Patches[i].ScreenCoordinates =
                            new Vector2(ScreenCoordinates.x + 2 * PatchMargin + 64.0f + RegionLineWidth,
                                ScreenCoordinates.y + regionMargin);
                    }
                    else
                    {
                        Patches[i].ScreenCoordinates =
                            new Vector2(ScreenCoordinates.x + 2 * PatchMargin + 64.0f + RegionLineWidth,
                                ScreenCoordinates.y + 2 * PatchMargin + 64.0f + RegionLineWidth);
                    }
                }
            }

            if (Type is RegionType.Vent or RegionType.Cave)
            {
                Patches[0].ScreenCoordinates = new Vector2(ScreenCoordinates.x + regionMargin,
                    ScreenCoordinates.y + regionMargin);

                // Caves or vents are the same depth as the adjacent patch
                Patches[0].Depth[0] = Patches[0].Adjacent.First().Depth[0];
                Patches[0].Depth[1] = Patches[0].Adjacent.First().Depth[1];
            }
        }
    }

    public void BuildRegion()
    {
        // Region size configuration
        Width += 64.0f + 2 * PatchMargin + RegionLineWidth;

        if (Type == RegionType.Continent)
        {
            Height = 64.0f + 2 * PatchMargin + RegionLineWidth;
            if (Patches.Count > 1)
                Width += 64.0f + PatchMargin;

            if (Patches.Count > 2)
                Height = 3 * PatchMargin + 2 * 64.0f + RegionLineWidth;
        }

        if (Type is RegionType.Ocean or RegionType.Sea)
        {
            Height += 64.0f * Patches.Count + (Patches.Count + 1) * PatchMargin + RegionLineWidth;
        }

        if (Type == RegionType.Vent)
        {
            Height = Width = 64 + 2 * PatchMargin + RegionLineWidth;

            var adjacent = Adjacent.First();
            ScreenCoordinates = adjacent.ScreenCoordinates + new Vector2(0, adjacent.Height) + new Vector2(0, 20);
        }

        if (Type == RegionType.Cave)
        {
            Height = Width = 64 + 2 * PatchMargin + RegionLineWidth;

            var adjacent = Adjacent.First();
            var adjacentPatch = Patches[0].Adjacent.First();
            if (adjacent.Type is RegionType.Sea or RegionType.Ocean)
            {
                ScreenCoordinates = adjacent.ScreenCoordinates +
                    new Vector2(adjacent.Width, adjacent.Patches.IndexOf(adjacentPatch) *
                        (64.0f + PatchMargin)) + new Vector2(20, 0);
            }

            if (adjacent.Type == RegionType.Continent)
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
        if (Type is RegionType.Ocean or RegionType.Sea)
        {
            foreach (var adjacent in Adjacent)
            {
                if (adjacent.Type == RegionType.Continent)
                {
                    var patchIndex = random.Next(0, adjacent.Patches.Count - 1);
                    LinkPatches(Patches[0], adjacent.Patches[patchIndex]);
                }

                if (adjacent.Type is RegionType.Sea or RegionType.Ocean)
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

        if (Type == RegionType.Continent)
        {
            foreach (var adjacent in Adjacent)
            {
                if (adjacent.Type == RegionType.Continent)
                {
                    var maxIndex = Math.Min(Patches.Count, adjacent.Patches.Count);
                    var patchIndex = random.Next(0, maxIndex);
                    LinkPatches(Patches[patchIndex], adjacent.Patches[patchIndex]);
                }
            }
        }
    }

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }

    private void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }
}
