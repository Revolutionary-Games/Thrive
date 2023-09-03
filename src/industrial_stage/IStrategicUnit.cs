using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   Interface for all strategic stage units that the player can command to move around implement
/// </summary>
public interface IStrategicUnit
{
    public string UnitName { get; }

    /// <summary>
    ///   The name of the unit shown by <see cref="StrategicUnitScreen{T}"/>
    /// </summary>
    [JsonIgnore]
    public string UnitScreenTitle { get; }

    /// <summary>
    ///   Queue of orders for this unit. Will be used in the future to visualize the orders the unit is performing
    /// </summary>
    public Deque<IUnitOrder> QueuedOrders { get; }
}

public static class StrategicUnitHelpers
{
    /// <summary>
    ///   Orders this unit to immediately perform some action and dismiss the order queue
    /// </summary>
    /// <param name="unit">The unit the order is for</param>
    /// <param name="order">The order to perform</param>
    public static void PerformOrder(this IStrategicUnit unit, IUnitOrder order)
    {
        unit.QueuedOrders.Clear();

        unit.QueueOrder(order);
    }

    /// <summary>
    ///   Queues an order to perform for this unit once done
    /// </summary>
    /// <param name="unit">The unit the order is for</param>
    /// <param name="order">The order</param>
    public static void QueueOrder(this IStrategicUnit unit, IUnitOrder order)
    {
        unit.QueuedOrders.AddToBack(order);
    }

    /// <summary>
    ///   Processes orders for a unit. Should only be called by the unit itself or a processing system that handles the
    ///   unit type
    /// </summary>
    /// <param name="unit">Unit to process orders for</param>
    /// <param name="delta">Time elapsed to make sure orders run at consistent speed</param>
    /// <typeparam name="TUnit">The unit type the order is for</typeparam>
    public static void ProcessOrderQueue<TUnit>(this TUnit unit, float delta)
        where TUnit : class, IStrategicUnit
    {
        if (unit.QueuedOrders.Count < 1)
            return;

        var order = (UnitOrderBase<TUnit>)unit.QueuedOrders[0];

        if (order.ProcessOrder(delta))
        {
            unit.QueuedOrders.RemoveFromFront();
        }
    }
}
