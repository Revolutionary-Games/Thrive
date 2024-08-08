namespace AutoEvo;

using System;

public static class CommonPressureFunctions
{
    public static int GetOrganelleCount(Species species)
    {
        if (species is MicrobeSpecies microbeSpecies)
            return microbeSpecies.Organelles.Count;

        if (species is EarlyMulticellularSpecies multicell)
        {
            var count = 0;

            foreach (var cell in multicell.Cells)
            {
                count += cell.Organelles.Count;
            }

            return count;
        }

        throw new ArgumentException("Unhandled species type passed to GetOrganelleCount");
    }
}
