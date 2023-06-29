/// <summary>
///   The ordered phases of phagocytosis process.
/// </summary>
public enum PhagocytosisPhase
{
    /// <summary>
    ///   Not being phagocytized in any way.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Engulfable is in the process of being moved into the cytoplasm to be stored.
    /// </summary>
    Ingestion,

    /// <summary>
    ///   Engulfable has been moved into the cytoplasm and is completely internalized. Digestion may begin.
    /// </summary>
    Ingested,

    /// <summary>
    ///   Engulfable is completely broken down.
    /// </summary>
    Digested,

    /// <summary>
    ///   Engulfable is in the process of being moved into the membrane layer for ejection.
    /// </summary>
    Exocytosis,

    /// <summary>
    ///   The expulsion of the engulfable into extracellular environment.
    /// </summary>
    Ejection,
}
