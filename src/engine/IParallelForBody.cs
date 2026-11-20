/// <summary>
///   Loop body for <see cref="TaskExecutor.ParallelFor{TBody}"/>
/// </summary>
/// <remarks>
///   <para>
///     Implement this on a struct so that the loop can run without boxing or delegate allocations.
///   </para>
/// </remarks>
public interface IParallelForBody
{
    /// <summary>
    ///   Processes a part of the loop
    /// </summary>
    /// <param name="fromInclusive">First index</param>
    /// <param name="toExclusive">Last index + 1 (value exclusive)</param>
    public void Execute(int fromInclusive, int toExclusive);
}
