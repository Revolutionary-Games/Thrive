using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumHelper
{
    /// <summary>
    ///   Fetches all CustomAttributes of type T defined on the enum value.
    /// </summary>
    /// <typeparam name="T">The Attribute type to be fetched</typeparam>
    /// <returns>Returns a list of Attributes of Type T</returns>
    public static IEnumerable<T> GetAttributes<T>(this Enum e)
        where T : Attribute
    {
        var memInfo = e.GetType().GetMember(e.ToString());
        var attrs = memInfo[0].GetCustomAttributes(typeof(T), false);
        return attrs.Cast<T>();
    }

    /// <summary>
    ///   Fetches one CustomAttributes of type T defined on the enum value.
    /// </summary>
    /// <typeparam name="T">The Attribute type to be fetched</typeparam>
    /// <returns>Returns one Attribute of Type T</returns>
    public static T GetAttribute<T>(this Enum e)
        where T : Attribute
    {
        return GetAttributes<T>(e).First();
    }
}
