using System.Collections.Generic;
using System.Linq;

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
        OrganellesRoot.Check(name);
        Organelles.ForEach(page => page.Check(name));
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

        public void Check(string name)
        {
            if (string.IsNullOrEmpty(InternalName))
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Page has no internal name");

            if (string.IsNullOrEmpty(Name))
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no name");

            if (string.IsNullOrEmpty(Url))
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no URL");

            if (Sections.Count < 1)
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no sections");

            if (Sections.Any(s => string.IsNullOrEmpty(s.SectionBody)))
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has an empty section");
        }
    }
}
