using System;
using DefaultEcs;

public static class TemporarySystemHelper
{
    private static readonly Lazy<World> LoadingWorld = new(() => new World(0));

    /// <summary>
    ///   Returns a world usable for temporary system creation to get JSON loading with child properties work
    /// </summary>
    /// <returns>
    ///   A world that is only safe to pass to the base constructor of systems that aren't going to be used
    /// </returns>
    public static World GetDummyWorldForLoad()
    {
        // Would be pretty nice to be able to access the internal worlds list to grab the first existing one from there
        // but a lazy variable should be fine enough for now that keeps the world used alive as long as the program
        // runs
        return LoadingWorld.Value;
    }
}
