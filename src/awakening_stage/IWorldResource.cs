using Godot;

public interface IWorldResource : IPlayerReadableName
{
    public PackedScene WorldRepresentation { get; }
}
