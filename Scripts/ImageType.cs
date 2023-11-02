namespace Scripts;

/// <summary>
///   Podman image type
/// </summary>
public enum ImageType
{
    /// <summary>
    ///   The main image used by the CI system
    /// </summary>
    CI,

    /// <summary>
    ///   Container where native dependencies are built for distribution
    /// </summary>
    NativeBuilder,
}
