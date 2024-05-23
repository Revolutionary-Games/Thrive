using Newtonsoft.Json;

public interface IDaylightInfo
{
    [JsonIgnore]
    public float DayFractionUntilNightStart { get; }

    [JsonIgnore]
    public float SecondsUntilNightStart { get; }
}

public class DummyLightCycle : IDaylightInfo
{
    // Start off at noon and just stay there
    public float DayFractionUntilNightStart { get; set; } = 0.5f;
    public float SecondsUntilNightStart { get; set; } = 180 / 2.0f;
}
