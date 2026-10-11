/// <summary>
///   Simple <see cref="IPlayerReadableName"/> for things that are just a label (for example "cell size" in
///   tolerance effect summaries)
/// </summary>
public class SimpleReadableName : IPlayerReadableName
{
    public SimpleReadableName(string readableName)
    {
        ReadableName = readableName;
    }

    public string ReadableName { get; }

    public override bool Equals(object? obj)
    {
        return obj is SimpleReadableName other && ReadableName == other.ReadableName;
    }

    public override int GetHashCode()
    {
        return ReadableName.GetHashCode();
    }

    public override string ToString()
    {
        return ReadableName;
    }
}