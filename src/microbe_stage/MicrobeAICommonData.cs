using System.Collections.Generic;

/// <summary>
///   Common MicrobeAI data shared by each instance. THIS MAY NOT BE MODIFIED OUTSIDE MicrobeAISystem!
/// </summary>
public class MicrobeAICommonData
{
    public MicrobeAICommonData(List<Microbe> allMicrobes, List<FloatingChunk> allChunks, CompoundCloudSystem clouds)
    {
        AllMicrobes = allMicrobes;
        AllChunks = allChunks;
        Clouds = clouds;
    }

    public List<Microbe> AllMicrobes { get; }
    public List<FloatingChunk> AllChunks { get; }
    public CompoundCloudSystem Clouds { get;  }
}
