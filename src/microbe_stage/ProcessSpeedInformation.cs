using System.Collections.Generic;

/// <summary>
///   Speed information of a process in specific patch. Used in the
///   editor to show info to the player.
/// </summary>
public class ProcessSpeedInformation
{
    public ProcessSpeedInformation(BioProcess process)
    {
        Process = process;
    }

    public BioProcess Process { get; }
    public float SpeedFactor { get; set; }

    public Dictionary<Compound, EnvironmentalInput> EnvironmentInputs { get; } =
        new Dictionary<Compound, EnvironmentalInput>();

    public Dictionary<Compound, CompoundAmount> OtherInputs { get; } =
        new Dictionary<Compound, CompoundAmount>();

    public Dictionary<Compound, CompoundAmount> Outputs { get; } =
        new Dictionary<Compound, CompoundAmount>();

    public class EnvironmentalInput : CompoundAmount
    {
        public EnvironmentalInput(Compound compound, float amount)
            : base(compound, amount)
        {
        }

        public float AvailableAmount { get; set; }
        public float AvailableRate { get; set; }
    }

    public class CompoundAmount
    {
        public CompoundAmount(Compound compound, float amount)
        {
            Compound = compound;
            Amount = amount;
        }

        public Compound Compound { get; }
        public float Amount { get; }
    }
}
