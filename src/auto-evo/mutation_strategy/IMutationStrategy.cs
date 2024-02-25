namespace AutoEvo
{
    using System.Collections.Generic;
    public interface IMutationStrategy<T>
        where T : Species
    {
        public abstract List<T> MutationsOf(T baseSpecies, MutationLibrary partList);
    }
}
