using Newtonsoft.Json;

/// <summary>
///   An effect diffusing compounds between patches
/// </summary>
[JSONDynamicTypeAllowed]
public class CompoundDiffusionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public CompoundDiffusionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        HandlePatchCompoundDiffusion();
    }

    private void HandlePatchCompoundDiffusion()
    {
        foreach (var patch in targetWorld.Map.Patches)
        {
            foreach (var adjacent in patch.Value.Adjacent)
            {
                foreach (var compound in patch.Value.Biome.Compounds)
                {
                    if (compound.Key is Compound.Sunlight or Compound.Temperature)
                        return;

                    var newConditions = compound.Value;

                    var fractionDensity =
                        (compound.Value.Density -
                            adjacent.Biome.Compounds[compound.Key].Density) / (patch.Value.Adjacent.Count + 1);
                    var fractionAmbient =
                        (compound.Value.Ambient -
                            adjacent.Biome.Compounds[compound.Key].Ambient) / (patch.Value.Adjacent.Count + 1);

                    newConditions.Density -= fractionDensity;
                    newConditions.Ambient -= fractionAmbient;

                    targetWorld.Map.Patches[patch.Key].Biome.ModifyLongTermCondition(compound.Key, newConditions);
                }
            }
        }
    }
}
