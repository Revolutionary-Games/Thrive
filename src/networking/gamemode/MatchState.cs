/// <summary>
///   Phase a networked match is in. Modes that don't use a phase stay in <see cref="Running"/>.
/// </summary>
public enum MatchState
{
    Lobby = 0,

    Countdown = 1,

    Running = 2,

    /// <summary>
    ///   Between rounds. In the arena this is when players are in the editor.
    /// </summary>
    Intermission = 3,

    /// <summary>
    ///   Final scores are shown before returning to the lobby or starting the next match
    /// </summary>
    Results = 4,
}
