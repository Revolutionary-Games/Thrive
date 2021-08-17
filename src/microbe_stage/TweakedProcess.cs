using System;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
public class TweakedProcess : ICloneable
{
    public float Rate;

    public TweakedProcess(BioProcess process, float rate = 1.0f)
    {
        Rate = rate;
        Process = process;
    }

    public BioProcess Process { get; }

    public object Clone()
    {
        return new TweakedProcess(Process, Rate);
    }
}
