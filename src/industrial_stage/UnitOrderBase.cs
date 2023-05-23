/// <summary>
///   Base class for strategic unit orders, given to <see cref="IStrategicUnit"/>
/// </summary>
/// <typeparam name="TUnit">The type of unit this order is for</typeparam>
public abstract class UnitOrderBase<TUnit> : IUnitOrder
{
    protected UnitOrderBase(TUnit unit)
    {
        Unit = unit;
    }

    public bool Completed { get; private set; }

    public TUnit Unit { get; }

    /// <summary>
    ///   Processes this order towards completion.
    /// </summary>
    /// <returns>True when complete</returns>
    public bool ProcessOrder(float delta)
    {
        if (Completed)
            return true;

        if (WorkOnOrder(delta))
        {
            Completed = true;
            return true;
        }

        return false;
    }

    protected abstract bool WorkOnOrder(float delta);
}
