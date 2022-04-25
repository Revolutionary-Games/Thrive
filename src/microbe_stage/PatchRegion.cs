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
    public string RegionType = null!;

    /// <summary>
    ///   Coordinates this patch is to be displayed in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; } = new(0, 0);

    public float PatchMargin = 6f;

    public float Height;

    public float Width;

    public List<Patch> Patches;

    public PatchRegion(int Id, LocalizedString name)
    {
        ID = Id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
    }

    public void AddPatch(Patch patch)
    {
        Patches.Add(patch);
    }

    public void Build()
    {
        Patches.Sort((x,y) => x.Depth[1].CompareTo(y.Depth[1]));
        
        for (int i = 0; i < Patches.Count - 1; i++)
        {
            if (RegionType == "sea" || RegionType == "ocean")
            {
                LinkPatches(Patches[i], Patches[i+1]);
            }

            if (RegionType == "continent")
            {
                for (int j = 0; j < 4; j++)
                {
                    if (j != i)
                    {
                        LinkPatches(Patches[i], Patches[j]);
                    }
                }

            }
        }

        // IF patches start at the same hieght make the region wider on the screen, 
        // and put the patches horizontally near eachother
        for (int i = 0; i < Patches.Count - 1; i++)
        {
            if (Patches[i].Depth[0] == Patches[i + 1].Depth[0])
            {
                Width += 64f * 2 * PatchMargin;
            }

            Patches[i + 1].ScreenCoordinates = new Vector2 (ScreenCoordinates.x, ScreenCoordinates.y + i * (64f + PatchMargin));
        }

        Height = 64f * Patches.Count + (Patches.Count + 1) * PatchMargin;

    }

    private void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }


}
