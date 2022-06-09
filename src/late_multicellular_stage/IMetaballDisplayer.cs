public interface IMetaballDisplayer<TMetaball>
    where TMetaball : Metaball
{
    /// <summary>
    ///   If set, overrides the alpha of the displayed metaballs
    /// </summary>
    public float? OverrideColourAlpha { get; set; }
}
