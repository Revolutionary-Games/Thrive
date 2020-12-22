using Godot;

/// <summary>
///   Defines properties of a membrane type
/// </summary>
public class MembraneType : IRegistryType
{
    public string NormalTexture;
    public string DamagedTexture;
    public float MovementFactor = 1.0f;
    public float OsmoregulationFactor = 1.0f;
    public float ResourceAbsorptionFactor = 1.0f;
    public int Hitpoints = 100;
    public float PhysicalResistance = 1.0f;
    public float ToxinResistance = 1.0f;
    public int EditorCost = 50;
    public bool CellWall = false;
    public float MovementWigglyness = 1.0f;

    public Texture LoadedNormalTexture;
    public Texture LoadedDamagedTexture;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(NormalTexture) || string.IsNullOrEmpty(DamagedTexture))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty normal or damaged texture");
        }

        var directory = new Directory();

        string[] membranes = { NormalTexture, DamagedTexture };

        foreach (var resource in membranes)
        {
            // When exported only the .import files exist, so this check is done accordingly
            if (!directory.FileExists(resource + ".import"))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Membrane uses non-existant image: " + resource);
            }
        }
    }

    /// <summary>
    ///   Resolves references to external resources so that during
    ///   runtime they don't need to be looked up
    /// </summary>
    public void Resolve()
    {
        LoadedNormalTexture = GD.Load<Texture>(NormalTexture);
        LoadedDamagedTexture = GD.Load<Texture>(DamagedTexture);
    }

    public void ApplyTranslations()
    {
    }
}
