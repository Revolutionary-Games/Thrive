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
        Update();
    }

    public void Update()
    {
        Patches.Sort((x,y) => x.Depth[1].CompareTo(y.Depth[1]));

        foreach (Patch patch in Patches)
        {
            
        }

        // IF patches start at the same hieght make the region wider on the screen, 
        // and put the patches horizontally near each
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

}
