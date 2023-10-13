using System.Collections.Generic;

/// <summary>
///   All wiki pages to be recreated in the Thriveopedia, grouped by type.
/// </summary>
public class GameWiki : IRegistryType
{
    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public Page OrganellesRoot { get; set; } = null!;

    public List<Page> Organelles { get; set; } = null!;

    public void ApplyTranslations()
    {
    }

    public void Check(string name)
    {
    }

    public class Page
    {
        public string Name { get; set; } = null!;

        public string InternalName { get; set; } = null!;

        public string Url { get; set; } = null!;

        public List<Section> Sections { get; set; } = null!;

        public class Section
        {
            public string? SectionHeading { get; set; }

            public string SectionBody { get; set; } = null!;
        }
    }
}
