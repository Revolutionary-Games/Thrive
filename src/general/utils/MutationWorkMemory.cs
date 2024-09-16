using System.Collections.Generic;

/// <summary>
///   Holds temporary working memory for mutations
/// </summary>
public class MutationWorkMemory
{
    public readonly List<Hex> WorkingMemory1 = new();
    public readonly List<Hex> WorkingMemory2 = new();
    public readonly HashSet<Hex> WorkingMemory3 = new();
    public readonly Queue<Hex> WorkingMemory4 = new();
}
