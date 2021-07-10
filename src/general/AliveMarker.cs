using System;
using Newtonsoft.Json;

/// <summary>
///   Holds information whether some other object is alive or not
/// </summary>
[JsonObject(IsReference = true)]
public sealed class AliveMarker
{
    private bool alive = true;

    [JsonIgnore]
    public bool Alive
    {
        get => alive;
        set
        {
            if (value == alive)
                return;

            if (value)
                throw new ArgumentException("Can't set a dead alive marker back to alive");

            alive = false;
        }
    }
}
