using System.Reflection;
using System.Runtime.CompilerServices;

public static class BindingExtensions
{
    public static int CountColonyMembers(this Microbe microbe, int runningValue = 0, bool fromAbove = false)
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

    public static double GetColonyValueAvg(this Microbe microbe, string property)
    {
        return GetColonyValueAvg(microbe, typeof(Microbe).GetProperty(property));
    }

    public static double GetColonyValueAvg(this Microbe microbe, PropertyInfo property)
    {
        return GetColonyValueSum(microbe, property) / CountColonyMembers(microbe);
    }

    public static double GetColonyValueSum(this Microbe microbe, string property)
    {
        return GetColonyValueSum(microbe, typeof(Microbe).GetProperty(property));
    }

    public static double GetColonyValueSum(
        this Microbe microbe, PropertyInfo property, bool fromAbove = false, double currValue = 0)
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

    public static T GetColonyValue<T>(this Microbe microbe, [CallerMemberName] string property = "")
    {
        if (!microbe.ColonyValues.ContainsKey(property))
        {
            microbe.ColonyValues[property] =
                microbe.Colony?.Master == null ? default : GetColonyValue<T>(microbe.Colony.Master.Microbe, property);
        }

        return (T)microbe.ColonyValues[property];
    }

    public static void SetColonyValue<T>(
        this Microbe microbe, T value, [CallerMemberName] string property = "", bool fromAbove = false)
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
