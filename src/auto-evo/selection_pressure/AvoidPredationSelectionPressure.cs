namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AvoidPredationSelectionPressure : SelectionPressure
{
    public Species Predator;
    public Patch Patch;
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

    private readonly float weight;

    public AvoidPredationSelectionPressure(Species predator, float weight, Patch patch) : base(weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
                AddOrganelleAnywhere.Direction.FRONT),
            new AddMultipleOrganelles(new List<AddOrganelleAnywhere>
            {
                new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                    AddOrganelleAnywhere.Direction.REAR),
                AddOrganelleAnywhere.ThatCreateCompound(ATP),
            }),
        })
    {
        Patch = patch;
        Predator = predator;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var predationScore = new PredationEffectivenessPressure(species, Patch, 1, cache)
            .FitnessScore((MicrobeSpecies)Predator, species);

        if (predationScore == 0)
        {
            return 1.0f * weight;
        }

        return 1 / predationScore * weight;
    }
}
