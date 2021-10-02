using AutoEvo;

public class PlayerMigration : IRunStep
{
    public PlayerMigration(Species player, SpeciesMigration migration)
    {
        Player = player;
        Migration = migration;
    }

    public Species Player { get; }
    public SpeciesMigration Migration { get; }

    public int TotalSteps => 1;
    public bool RunStep(RunResults results)
    {
        results.AddMigrationResultForSpecies(Player, Migration);
        return true;
    }
}
