using System.Collections.Generic;

public interface IMetaballDisplayer<TMetaball>
    where TMetaball : Metaball
{
    /// <summary>
    ///   If set, overrides the alpha of the displayed metaballs
    /// </summary>
    public float? OverrideColourAlpha { get; set; }

    /// <summary>
    ///   If set to false, this will not draw / update hierarchy lines (but any already drawn lines might still be
    ///   visible, so this should be set to false before drawing anything for the first time)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Not supported by all display types, in which case they do nothing if this is set to true.
    ///   </para>
    /// </remarks>
    public bool DisplayHierarchyLines { get; set; }

    public bool Visible { get; set; }

    public void DisplayFromLayout(IReadOnlyCollection<TMetaball> layout);
}
