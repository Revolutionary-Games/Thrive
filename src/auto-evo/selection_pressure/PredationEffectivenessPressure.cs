namespace AutoEvo;

using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class PredationEffectivenessPressure : SelectionPressure
{
    [JsonProperty]
    public readonly Species Prey;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public PredationEffectivenessPressure(Species prey, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Compound.Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent, CommonMutationFunctions.Direction.Front),
            new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                CommonMutationFunctions.Direction.Rear),
            new AddOrganelleAnywhere(organelle => organelle.HasSlimeJetComponent,
                CommonMutationFunctions.Direction.Rear),
            new AddOrganelleAnywhere(organelle => organelle.HasLysosomeComponent),
            new MoveOrganelleBack(organelle => organelle.HasSlimeJetComponent),
            new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
            new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(0.5f)),
            new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
                new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CHITINASE_ENZYME))),
            new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
                new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CELLULASE_ENZYME))),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -150.0f),
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType("single"),
        ])
    {
        Prey = prey;
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        // No Cannibalism
        // Compared by ID here to make sure temporary species variants are not allowed to predate themselves
        if (species.ID == Prey.ID)
        {
            return 0.0f;
        }

        var predatorScore = cache.GetPredationScore(species, Prey, patch.Biome);
        var reversePredatorScore = cache.GetPredationScore(Prey, species, patch.Biome);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return 0.0f;
        }

        return predatorScore;
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.SpeciesInPatch.TryGetValue(Prey, out long population) || population <= 0)
            return 0;

        return population * Prey.GetPredationTargetSizeFactor() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("PREDATION_FOOD_SOURCE", Prey.FormattedNameBbCode);
    }

    public override string ToString()
    {
        return $"{Name} ({Prey.FormattedName})";
    }
}
