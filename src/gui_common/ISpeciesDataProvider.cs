public interface ISpeciesDataProvider
{
    public Species? GetActiveSpeciesData(uint speciesId, bool printErrors = true);
}
