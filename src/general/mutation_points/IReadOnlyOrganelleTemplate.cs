public interface IReadOnlyOrganelleTemplate : IReadOnlyPositionedOrganelle
{
    public Compound GetActiveTargetCompound();

    public Enzyme? GetActiveTargetEnzyme(string internalName);

    public float GetActiveToxicity();

    public ToxinType GetActiveToxin();

    public Species? GetActiveTargetSpecies();

    public OrganelleTemplate Clone();
}
