using System.Collections.Generic;
using System.Linq;
using UnlockConstraints;

/// <summary>
///   A player can unlock the organelle if all of these constraints are satisfied. Specified in organelles.json.
/// </summary>
public class OrganelleUnlockConstraints
{
    /// <summary>
    ///   The number of microbes engulfed by the player is above the specified amount.
    /// </summary>
    public int? EngulfedMicrobesAbove { get; set; }

    /// <summary>
    ///   The number of times the player has died is above a specified amount.
    /// </summary>
    public int? PlayerDeathsAbove { get; set; }

    /// <summary>
    ///   The player species produces more than a specified amount of APT.
    /// </summary>
    public float? AptAbove { get; set; }

    /// <summary>
    ///   The player species produces a specified amount more APT than it uses.
    /// </summary>
    public float? ExcessAptAbove { get; set; }

    /// <summary>
    ///   The player species has a base speed that is slower than a certain speed.
    /// </summary>
    public float? BaseSpeedBelow { get; set; }

    /// <summary>
    ///   The player species has had a specific organelle for some number of generations.
    /// </summary>
    public ReproducedWith? ReproducedWith { get; set; }

    /// <summary>
    ///   The player has reproduced in a specific named biome.
    /// </summary>
    public string? ReproduceInBiome { get; set; }

    /// <summary>
    ///   The player's current biome has a certain amount of a compound.
    /// </summary>
    public BiomeCompound? BiomeCompound { get; set; }

    /// <summary>
    ///   Are all of the specified unlock constraints satisfied?
    /// </summary>
    public bool Satisfied(GameWorld world)
    {
        if (world.PlayerSpecies is not MicrobeSpecies playerSpecies)
            return false;

        var energyBalance = ProcessSystem.ComputeEnergyBalance(playerSpecies.Organelles, world.Map.CurrentPatch!.Biome,
            playerSpecies.MembraneType, true, world.WorldSettings, CompoundAmountType.Current);

        return UnlockConstraints().All(constraint => constraint.Satisfied(world, energyBalance));
    }

    /// <summary>
    ///   Display the requirements as a string.
    /// </summary>
    public void Tooltip(GameWorld world, LocalizedStringBuilder value)
    {
        if (world.PlayerSpecies is not MicrobeSpecies playerSpecies)
        {
            value.Append(new LocalizedString("MUST_BE_A_MICROBE"));
            return;
        }

        var energyBalance = ProcessSystem.ComputeEnergyBalance(playerSpecies.Organelles, world.Map.CurrentPatch!.Biome,
            playerSpecies.MembraneType, true, world.WorldSettings, CompoundAmountType.Current);

        var first = true;
        foreach (var constraint in UnlockConstraints())
        {
            if (!first)
                value.Append(new LocalizedString("UNLOCK_AND"));
            first = false;

            if (constraint.Satisfied(world, energyBalance))
                value.Append("[color=green]");
            else
                value.Append("[color=red]");

            constraint.Tooltip(world, energyBalance, value);

            value.Append("[/color]");
        }
    }

    /// <summary>
    ///   An iterable over all of the unlock constraints that have been set.
    /// </summary>
    private IEnumerable<IUnlockConstraint> UnlockConstraints()
    {
        if (EngulfedMicrobesAbove is not null)
            yield return new EngulfedMicrobesAbove { Microbes = EngulfedMicrobesAbove.Value };

        if (PlayerDeathsAbove is not null)
            yield return new PlayerDeathsAbove { Deaths = PlayerDeathsAbove.Value };

        if (AptAbove is not null)
            yield return new AptAbove { Atp = AptAbove.Value };

        if (ExcessAptAbove is not null)
            yield return new ExcessAptAbove { Excess = ExcessAptAbove.Value };

        if (BaseSpeedBelow is not null)
            yield return new BaseSpeedBelow { Speed = BaseSpeedBelow.Value };

        if (ReproducedWith is not null)
            yield return ReproducedWith;

        if (ReproduceInBiome is not null)
            yield return new ReproduceInBiome { Biome = SimulationParameters.Instance.GetBiome(ReproduceInBiome) };

        if (BiomeCompound is not null)
            yield return BiomeCompound;
    }
}
