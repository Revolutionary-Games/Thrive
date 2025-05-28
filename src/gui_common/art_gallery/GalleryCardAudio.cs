﻿using System;
using Godot;

/// <summary>
///   Audio type art gallery item
/// </summary>
public partial class GalleryCardAudio : GalleryCard, IGalleryCardPlayback
{
#pragma warning disable CA2213
    [Export]
    private PlaybackControls playbackControls = null!;

    private AudioStreamPlayer? ownPlayer;
#pragma warning restore CA2213

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    /// <summary>
    ///   NOTE: Manipulating playback shouldn't be done directly through here, instead use the provided methods
    ///   so that controls could be updated accordingly.
    /// </summary>
    public AudioStreamPlayer Player
    {
        get
        {
            EnsurePlayerExist();
            return ownPlayer!;
        }
        set
        {
            ownPlayer = value;
            UpdatePlaybackBar();
        }
    }

    public bool Playing => playbackControls.Playing;

    public override void _Ready()
    {
        base._Ready();

        EnsurePlayerExist();
    }

    public void StartPlayback()
    {
        playbackControls.StartPlayback();
    }

    public void StopPlayback()
    {
        playbackControls.StopPlayback();
    }

    private void EnsurePlayerExist()
    {
        if (ownPlayer == null)
        {
            ownPlayer = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(Asset!.ResourcePath), VolumeLinear = 0 };
            UpdatePlaybackBar();
            AddChild(ownPlayer);
        }
    }

    private void UpdatePlaybackBar()
    {
        playbackControls.AudioPlayer = ownPlayer;
    }

    private void OnStarted()
    {
        PlaybackStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopped()
    {
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }
}
