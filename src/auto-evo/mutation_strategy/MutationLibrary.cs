namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MutationLibrary
    {
        public Dictionary<string, OrganelleDefinition> PermittedOrganelleDefinitions = new();

        public MutationLibrary(MicrobeSpecies microbeSpecies)
        {
            var microbeOrganelles = microbeSpecies.Organelles.Select(organelle => organelle.Definition).Distinct();
            foreach (var organelleDefinition in SimulationParameters.Instance.GetAllOrganelles())
            {
                var shouldAdd = true;

                if (organelleDefinition.RequiresNucleus && microbeSpecies.IsBacteria)
                {
                    shouldAdd = false;
                }

                if (organelleDefinition.Unique && microbeOrganelles.ToList().Contains(organelleDefinition))
                {
                    shouldAdd = false;
                }

                // TODO: Make this use a shared random and based on a property in the organelle definition
                if (new Random().NextDouble() < 0.6)
                {
                    shouldAdd = false;
                }

                if (shouldAdd)
                {
                    PermittedOrganelleDefinitions.Add(organelleDefinition.Name, organelleDefinition);
                }
            }
        }

        public OrganelleDefinition? GetOrganelleType(string name)
        {
            if (PermittedOrganelleDefinitions.TryGetValue(name, out OrganelleDefinition? value))
            {
                return value;
            }

            return null;
        }

        public IEnumerable<OrganelleDefinition> GetAllOrganelles()
        {
            return PermittedOrganelleDefinitions.Values;
        }
    }
}
