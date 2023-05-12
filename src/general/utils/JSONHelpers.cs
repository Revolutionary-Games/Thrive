using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JSONHelpers
{
    /// <summary>
    ///   Converts the value. Throws if can't convert
    /// </summary>
    /// <param name="value">The JSON value to convert</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>The converted value</returns>
    public static T ValueNotNull<T>(this IEnumerable<JToken> value)
        where T : class
    {
        var result = value.Value<T>();

        if (result == null)
            throw new JsonException("JSON value conversion to target type failed");

        return result;
    }
}
