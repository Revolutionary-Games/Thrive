using System.Collections.Generic;

/// <summary>
///   Background in the microbe stage, needs to have 4 layers (textures)
/// </summary>
public class Background : IRegistryType
{
    public List<string> Textures;

    public void Check(string name)
    {
        if (Textures.Count != 4)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Background needs 4 layers");
        }
    }
}
