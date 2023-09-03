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
}
