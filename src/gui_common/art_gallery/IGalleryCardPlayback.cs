using System;

public interface IGalleryCardPlayback
{
    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    public bool Playing { get; }

    public void StartPlayback();

    public void StopPlayback();
}
