/// <summary>
///   Copies properties from a save loaded object copy
/// </summary>
public static class SaveApplyHelper
{
    /// <summary>
    ///   Copies the properties and fields that the Thrive JSON converter saves into JSON
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This doesn't clone cloneable properties, so the source object should not be used anymore after this as
    ///     reference properties can get changed in it after target starts running.
    ///   </para>
    /// </remarks>
    /// <param name="target">Object to set the properties on</param>
    /// <param name="source">Object to copy things from</param>
    /// <typeparam name="T">The type of object to handle</typeparam>
    public static void CopyJSONSavedPropertiesAndFields<T>(T target, T source)
    {
        foreach (var field in BaseThriveConverter.FieldsOf(target))
        {
            if (IsNameLoadedFromSaveName(field.Name))
                continue;

            field.SetValue(target, field.GetValue(source));
        }

        foreach (var property in BaseThriveConverter.PropertiesOf(target))
        {
            if (IsNameLoadedFromSaveName(property.Name))
                continue;

            var set = property.GetSetMethodOnDeclaringType();

            set.Invoke(target, new object[] { property.GetValue(source) });
        }
    }

    /// <summary>
    ///   Used to ignore overwriting the property saying an object was loaded from a save
    /// </summary>
    /// <param name="name">Name to check against</param>
    /// <returns>True if should be skipped</returns>
    public static bool IsNameLoadedFromSaveName(string name)
    {
        return name == "IsLoadedFromSave" || name == "isLoadedFromSave";
    }
}
