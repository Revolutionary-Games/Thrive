/// <summary>
///   Various items need to be marked to consider them still used (or for other purposes)
/// </summary>
public interface IMarkable
{
    /// <summary>
    ///   True when marked
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This could be in most cases a <see cref="Newtonsoft.Json.JsonIgnoreAttribute"/> but in case some use needs
    ///     persistent marking this is not ignored when saving / loading.
    ///   </para>
    /// </remarks>
    public bool Marked { get; set; }
}
