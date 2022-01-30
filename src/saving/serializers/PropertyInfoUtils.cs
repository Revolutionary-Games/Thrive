using System.Reflection;

/// <summary>
///   Extensions for property info
/// </summary>
public static class PropertyInfoUtils
{
    public static MethodInfo? GetSetMethodOnDeclaringType(this PropertyInfo propertyInfo)
    {
        var methodInfo = propertyInfo.GetSetMethod(true);
        return methodInfo ?? propertyInfo.DeclaringType?.GetProperty(propertyInfo.Name)?.GetSetMethod(true);
    }
}
