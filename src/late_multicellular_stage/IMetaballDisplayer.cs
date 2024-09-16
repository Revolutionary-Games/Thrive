using System.Collections.Generic;

public interface IMetaballDisplayer<TMetaball>
    where TMetaball : Metaball
{
    /// <summary>
    ///   If set, overrides the alpha of the displayed metaballs
    /// </summary>
    public float? OverrideColourAlpha { get; set; }

    public bool Visible { get; set; }

    public void DisplayFromLayout(IReadOnlyCollection<TMetaball> layout);
}
