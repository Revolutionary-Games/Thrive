﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   This contains the single game settings.
///   This is recreated when starting a new game
/// </summary>
public class GameProperties
{
    private readonly Dictionary<string, bool> lockedOrganelles =
        new Dictionary<string, bool>();
    private bool freeBuild = false;

    private GameProperties()
    {
        GameWorld = new GameWorld(new WorldGenerationSettings());
    }

    /// <summary>
    ///   The world this game is played in. Has all the species and map data
    /// </summary>
    public GameWorld GameWorld { get; private set; }

    /// <summary>
    ///   When true the player is in freebuild mode and various things
    ///   should be disabled / different.
    /// </summary>
    public bool FreeBuild
    {
        get
        {
            return freeBuild;
        }
    }

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
        }

        return game;
    }

    /// <summary>
    ///   Returns whether a key has a true bool set to it
    /// </summary>
    public bool IsBoolSet(string key)
    {
        return lockedOrganelles.ContainsKey(key) && lockedOrganelles[key];
    }

    /// <summary>
    ///   Binds a string to a bool
    /// </summary>
    public void SetBool(string key, bool value)
    {
        lockedOrganelles[key] = value;
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
