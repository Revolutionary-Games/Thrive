using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class AscensionComponent : SpaceStructureComponent
{
    [JsonProperty]
    private readonly float energyRequired;

    private readonly WorldResource energyResource;

    private bool missingEnergy = true;

    public AscensionComponent(PlacedSpaceStructure owningStructure, float energyRequired) : base(owningStructure)
    {
        this.energyRequired = energyRequired;

        energyResource = SimulationParameters.Instance.GetWorldResource("energy");
    }

    public override void ProcessSpace(float delta, ISocietyStructureDataAccess dataAccess)
    {
        missingEnergy = dataAccess.SocietyResources.GetAvailableAmount(energyResource) < energyRequired;
    }

    public override void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
        base.GetExtraAvailableActions(result);

        if (missingEnergy)
        {
            result.Add((InteractionType.ActivateAscension,
                TranslationServer.Translate("INTERACTION_ACTIVATE_ASCENSION_MISSING_ENERGY")));
        }
        else
        {
            result.Add((InteractionType.ActivateAscension, null));
        }
    }

    public override bool PerformExtraAction(InteractionType interactionType)
    {
        if (interactionType != InteractionType.ActivateAscension)
            return false;

        // TODO: actually confirm there's enough energy as the player energy amount could have fallen since the
        // action allowed status changed

        GD.Print("Ascension gate is triggered");

        // TODO: a cleaner way to do this
        var stage = owningStructure.FirstAncestorOfType<SpaceStage>();

        if (stage == null)
        {
            GD.PrintErr("Could not find parent space stage of ascension component to perform ascension");
            return true;
        }

        stage.OnStartAscension(owningStructure);
        return true;
    }
}

public class AscensionComponentFactory : ISpaceStructureComponentFactory
{
    [JsonProperty]
    public float EnergyRequired { get; private set; }

    public SpaceStructureComponent Create(PlacedSpaceStructure owningStructure)
    {
        return new AscensionComponent(owningStructure, EnergyRequired);
    }

    public void Check(string name)
    {
        if (EnergyRequired < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Energy needed to ascend needs to be at least 1");
        }
    }
}
