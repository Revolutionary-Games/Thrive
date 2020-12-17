using System.Reflection;
using System.Runtime.CompilerServices;

public static class BindingExtensions
{
    /// <summary>
    ///   Counts the members of my colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <returns>Returns the number of colony members. Returns 0 if the microbe is not in a colony.</returns>
    public static int CountColonyMembers(this Microbe microbe)
    {
        return CountColonyMembers(microbe, 0, false);
    }

    /// <summary>
    ///   Gets the average value of the property across the whole colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the average value across the colony.</returns>
    public static double GetColonyValueAvg(this Microbe microbe, [CallerMemberName] string property = "")
    {
        return GetColonyValueAvg(microbe, typeof(Microbe).GetProperty(property));
    }

    /// <summary>
    ///   Gets the average value of the property across the whole colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the average value across the colony.</returns>
    public static double GetColonyValueAvg(this Microbe microbe, PropertyInfo property)
    {
        return GetColonyValueSum(microbe, property) / CountColonyMembers(microbe);
    }

    /// <summary>
    ///   Gets the sum of the values of the properties across the whole colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the sum across the colony.</returns>
    public static double GetColonyValueSum(this Microbe microbe, [CallerMemberName] string property = "")
    {
        return GetColonyValueSum(microbe, typeof(Microbe).GetProperty(property));
    }

    /// <summary>
    ///   Gets the sum of the values of the properties across the whole colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the sum across the colony.</returns>
    public static double GetColonyValueSum(this Microbe microbe, PropertyInfo property)
    {
        return GetColonyValueSum(microbe, property, false, 0);
    }

    /// <summary>
    ///   Gets the value from the colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the value.</returns>
    /// <typeparam name="T">The type of the value</typeparam>
    public static T GetColonyValue<T>(this Microbe microbe, [CallerMemberName] string property = "")
    {
        if (!microbe.ColonyValues.ContainsKey(property))
        {
            microbe.ColonyValues[property] =
                microbe.Colony?.Master == null ? default : GetColonyValue<T>(microbe.Colony.Master.Microbe, property);
        }

        return (T)microbe.ColonyValues[property];
    }

    /// <summary>
    ///   Sets the value to the colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="value">The new value for the property</param>
    /// <param name="property">The property in question</param>
    /// <typeparam name="T">The type of the value</typeparam>
    public static void SetColonyValue<T>(this Microbe microbe, T value, [CallerMemberName] string property = "")
    {
        SetColonyValue(microbe, value, property, false);
    }

    // --- private recursive methods --- //
    private static int CountColonyMembers(this Microbe microbe, int runningValue, bool fromAbove)
    {
        if (microbe.Colony == null)
            return 0;

        if (microbe.Colony.Master == null || fromAbove)
        {
            runningValue++;

            foreach (var colonyMember in microbe.Colony.BindingTo)
                runningValue = CountColonyMembers(colonyMember.Microbe, runningValue, true);

            return runningValue;
        }

        return CountColonyMembers(microbe.Colony.Master.Microbe, 0, false);
    }

    private static double GetColonyValueSum(this Microbe microbe, PropertyInfo property, bool fromAbove,
        double currValue)
    {
        var myValue = (double)property.GetValue(microbe);
        if (microbe.Colony == null)
            return myValue;

        if (microbe.Colony.Master == null || fromAbove)
        {
            currValue += myValue;

            foreach (var colonyMember in microbe.Colony.BindingTo)
                currValue = GetColonyValueSum(colonyMember.Microbe, property, true, currValue);

            return currValue;
        }

        return GetColonyValueSum(microbe.Colony.Master.Microbe, property, false, currValue);
    }

    private static void SetColonyValue<T>(this Microbe microbe, T value, string property, bool fromAbove)
    {
        microbe.ColonyValues[property] = value;

        if (microbe.Colony == null)
            return;

        if (microbe.Colony.Master == null || fromAbove)
        {
            foreach (var colonyMember in microbe.Colony.BindingTo)
                SetColonyValue(colonyMember.Microbe, value, property, true);
        }
        else
        {
            SetColonyValue(microbe.Colony.Master.Microbe, value, property, false);
        }
    }
}
