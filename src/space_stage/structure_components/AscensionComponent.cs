using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class AscensionComponent : SpaceStructureComponent
{
    [JsonProperty]
    private readonly float energyRequired;

    private readonly WorldResource energyResource;

    private bool missingEnergy = true;

    public AscensionComponent(float energyRequired)
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
        if (interactionType == InteractionType.ActivateAscension)
        {
            // TODO: actually confirm there's enough energy as the player energy amount could have fallen since the
            // action allowed status changed

            GD.Print("Ascension gate is activated");

            GD.Print("TODO: implement ascension");
            return true;
        }

        return base.PerformExtraAction(interactionType);
    }
}

public class AscensionComponentFactory : ISpaceStructureComponentFactory
{
    [JsonProperty]
    public float EnergyRequired { get; private set; }

    public SpaceStructureComponent Create()
    {
        return new AscensionComponent(EnergyRequired);
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
