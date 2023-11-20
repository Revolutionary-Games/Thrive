/// <summary>
///   A photographable that requires an <see cref="IWorldSimulation"/> to create photographable graphics for an object
/// </summary>
public interface ISimulationPhotographable : IPhotographable<IWorldSimulation>
{
    public enum SimulationType
    {
        MicrobeGraphics,
    }

    public SimulationType SimulationToPhotograph { get; }

    public void SetupWorldEntities(IWorldSimulation worldSimulation);
    public bool StateHasStabilized(IWorldSimulation worldSimulation);
}
