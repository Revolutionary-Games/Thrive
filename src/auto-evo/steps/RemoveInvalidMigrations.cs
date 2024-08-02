namespace AutoEvo;

using System.Collections.Generic;

public class RemoveInvalidMigrations : IRunStep
{
    private readonly IReadOnlyCollection<Species> speciesToCheck;

    public RemoveInvalidMigrations(IReadOnlyCollection<Species> speciesToCheck)
    {
        this.speciesToCheck = speciesToCheck;
    }

    public int TotalSteps => 1;
    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        foreach (var species in speciesToCheck)
        {
            results.RemoveMigrationsForSplitPatches(species);
        }

        return true;
    }
}
