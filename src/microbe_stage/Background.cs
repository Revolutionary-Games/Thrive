using System.Collections.Generic;
using Godot;

/// <summary>
///   Background in the microbe stage, needs to have 4 layers (textures)
/// </summary>
public class Background : IRegistryType
{
    public List<string> Textures;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Textures.Count != 4)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Background needs 4 layers");
        }
    }

    /// <summary>
    ///   Checks that resource paths are valid. This doesn't preload the images as they are big and there are a lot of
    ///   them.
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        var directory = new Directory();

        foreach (var resource in Textures)
        {
            // When exported only the .import files exist, so this check is done accordingly
            if (!directory.FileExists(resource + ".import"))
            {
                throw new InvalidRegistryData(InternalName, this.GetType().Name,
                    "Background contains non-existant image: " + resource);
            }
        }
    }
}
