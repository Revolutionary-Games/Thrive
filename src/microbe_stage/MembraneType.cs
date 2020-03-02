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

    public MembraneType()
    {
    }

    public void Check(string name)
    {
        if (NormalTexture == string.Empty || DamagedTexture == string.Empty)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Empty normal or damaged texture");
        }
    }
}
