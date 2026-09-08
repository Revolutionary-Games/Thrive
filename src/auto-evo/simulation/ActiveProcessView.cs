namespace AutoEvo;

using System.Collections.Generic;

/// <summary>
///   Read access to a cache-owned process list. Elements are returned by value and the backing list
///   is never exposed. Registry process definitions retain their existing shared identity.
///   The cache must not modify or reuse the backing list after publishing this view, even after Clear.
/// </summary>
internal readonly struct ActiveProcessView
{
    private readonly List<TweakedProcess> processes;

    public ActiveProcessView(List<TweakedProcess> processes)
    {
        this.processes = processes;
    }

    public int Count => processes.Count;

    public TweakedProcess this[int index] => processes[index];

    public List<TweakedProcess>.Enumerator GetEnumerator()
    {
        return processes.GetEnumerator();
    }

    public List<TweakedProcess> ToMutableCopy()
    {
        return new List<TweakedProcess>(processes);
    }
}
