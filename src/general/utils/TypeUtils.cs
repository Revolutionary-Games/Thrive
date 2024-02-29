using System;

/// <summary>
///   Helpers for Type
/// </summary>
public static class TypeUtils
{
    /// <summary>
    ///   Gets an assembly qualified name without the assembly version, for use with JSON serialization
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>The only assembly qualified name without version</returns>
    public static string AssemblyQualifiedNameWithoutVersion(this Type type)
    {
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    /// <summary>
    ///   Generic-aware IsAssignableTo that can take in generic type definitions
    /// </summary>
    /// <param name="givenType">The type to check if it can be assigned to the generic</param>
    /// <param name="genericType">The generic type to check for example <c>System&lt;&gt;</c></param>
    /// <returns>True if assignable</returns>
    public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
    {
        if (genericType.IsAssignableFrom(givenType))
            return true;

        // Approach from (modified quite a bit): https://stackoverflow.com/a/5461399/4371508

        foreach (var type in givenType.GetInterfaces())
        {
            if (type.IsGenericType && genericType.IsAssignableFrom(type.GetGenericTypeDefinition()))
                return true;
        }

        if (givenType.IsGenericType && genericType.IsAssignableFrom(givenType.GetGenericTypeDefinition()))
            return true;

        var baseType = givenType.BaseType;
        if (baseType == null)
            return false;

        return IsAssignableToGenericType(baseType, genericType);
    }
}
