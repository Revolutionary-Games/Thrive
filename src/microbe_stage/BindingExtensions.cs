using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class BindingExtensions
{
    /// <summary>
    ///   Get all the members of the colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <returns>A list containing all members.</returns>
    public static List<Microbe> GetAllColonyMembers(this Microbe microbe)
    {
        if (microbe.Colony?.AllMembersCache != null)
            return microbe.Colony.AllMembersCache;

        var result = new List<Microbe>();
        GetAllColonyMembers(microbe, result, false);

        if (microbe.Colony == null)
            return result;

        foreach (var item in result)
            item.Colony.AllMembersCache = result;

        return result;
    }

    /// <summary>
    ///   Counts the members of my colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <returns>Returns the number of colony members. Returns 0 if the microbe is not in a colony.</returns>
    public static int CountColonyMembers(this Microbe microbe)
    {
        return GetAllColonyMembers(microbe).Count;
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
        return GetAllColonyMembers(microbe).Average(p => (double)property.GetValue(p));
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
        return GetAllColonyMembers(microbe).Sum(p => (double)property.GetValue(p));
    }

    /// <summary>
    ///   Gets the value from the colony.
    /// </summary>
    /// <param name="microbe">The microbe</param>
    /// <param name="defaultValue">The default value if the property was not defined before</param>
    /// <param name="property">The property in question</param>
    /// <returns>Returns the value.</returns>
    /// <typeparam name="T">The type of the value</typeparam>
    public static T GetColonyValue<T>(this Microbe microbe, T defaultValue = default, [CallerMemberName] string property = "")
    {
        if (!microbe.ColonyValues.ContainsKey(property))
        {
            microbe.ColonyValues[property] =
                microbe.Colony?.Master == null ? defaultValue : GetColonyValue(microbe.Colony.Master.Microbe, defaultValue, property);
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
    private static void GetAllColonyMembers(this Microbe microbe, ICollection<Microbe> current, bool fromAbove)
    {
        if (microbe.Colony == null)
        {
            current.Add(microbe);
            return;
        }

        if (microbe.Colony.Master == null || fromAbove)
        {
            current.Add(microbe);
            foreach (var colony in microbe.Colony.BindingTo)
                GetAllColonyMembers(colony.Microbe, current, true);
        }
        else
        {
            GetAllColonyMembers(microbe.Colony.Master.Microbe, current, false);
        }
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
