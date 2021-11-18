/// <summary>
///   This class provides an interface for mods to interact with the game through an API that will try to stay stable
///   between game versions
/// </summary>
/// <remarks>
///   <para>
///     Direct access to other game classes and code is allowed (and not really possible to block) from mods, but
///     the code might change drastically between versions and often break mods. As such this class collects some
///     operations mods are likely want to do and provides a way to do them in a way that won't be broken each
///     new release.
///   </para>
/// </remarks>
public class ModInterface
{
}
