/// <summary>
///   How stored compounds are handled on reproduction. Don't reorder this enum as it will break the GUI and saves
/// </summary>
public enum ReproductionCompoundHandling
{
    SplitWithSister = 0,
    KeepAsIs,
    TopUpWithInitial,
    TopUpOnPatchChange,
}
