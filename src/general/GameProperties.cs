using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   This contains the single game settings.
///   This is recreated when starting a new game
/// </summary>
[JsonObject(IsReference = true)]
public class GameProperties
{
    [JsonProperty]
    private readonly Dictionary<string, bool> setBoolStatuses = new Dictionary<string, bool>();

    [JsonProperty]
    private bool freeBuild;

    private GameProperties()
    {
        GameWorld = new GameWorld(new WorldGenerationSettings());
        TutorialState = new TutorialState();
    }

    /// <summary>
    ///   The world this game is played in. Has all the species and map data
    /// </summary>
    [JsonProperty]
    public GameWorld GameWorld { get; private set; }

    /// <summary>
    ///   When true the player is in freebuild mode and various things
    ///   should be disabled / different.
    /// </summary>
    [JsonIgnore]
    public bool FreeBuild => freeBuild;

    /// <summary>
    ///   The tutorial state for this game
    /// </summary>
    [JsonProperty]
    public TutorialState TutorialState { get; private set; }

    /// <summary>
    ///   Starts a new game in the microbe stage
    /// </summary>
    public static GameProperties StartNewMicrobeGame(bool freebuild = false)
    {
        var game = new GameProperties();

        if (freebuild)
        {
            game.EnterFreeBuild();
            game.GameWorld.GenerateRandomSpeciesForFreeBuild();
            game.TutorialState.Enabled = false;
        }

        return game;
    }

    /// <summary>
    ///   Returns whether a key has a true bool set to it
    /// </summary>
    public bool IsBoolSet(string key)
    {
        return setBoolStatuses.ContainsKey(key) && setBoolStatuses[key];
    }

    /// <summary>
    ///   Binds a string to a bool
    /// </summary>
    public void SetBool(string key, bool value)
    {
        setBoolStatuses[key] = value;
    }

    /// <summary>
    ///   Enters free build mode. There is purposefully no way to undo
    ///   this other than starting a new game.
    /// </summary>
    public void EnterFreeBuild()
    {
        GD.Print("Entering freebuild mode");
        freeBuild = true;
    }
}
