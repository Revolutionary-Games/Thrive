using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages playing music. Autoload singleton
/// </summary>
public class Jukebox : Node
{
    /// <summary>
    ///   Lists of music
    /// </summary>
    private readonly Dictionary<string, MusicCategory> categories;

    private readonly List<AudioStreamPlayer> audioPlayers = new List<AudioStreamPlayer>();

    /// <summary>
    ///   Loads the music categories and prepares to play them
    /// </summary>
    public Jukebox()
    {
        categories = SimulationParameters.Instance.GetMusicCategories();
    }

    public override void _Ready()
    {
        // Preallocate one audio stream player, due to the dynamic number of simultaneous tracks to play this is a list
        audioPlayers.Add(new AudioStreamPlayer());
    }

    public override void _Process(float delta)
    {
    }
}
