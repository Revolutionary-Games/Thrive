/// <summary>
///   A completed generation's timings and detached domain state, suitable for command-line output.
/// </summary>
public sealed class AutoEvoGenerationResult
{
    public required AutoEvoWorldState Input { get; init; }
    public required AutoEvoWorldState Output { get; init; }
    public required double ComputeSeconds { get; init; }
    public required double WallSeconds { get; init; }
}
