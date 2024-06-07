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

    public Page StagesRoot { get; set; } = null!;

    public List<Page> Stages { get; set; } = null!;

    public Page MechanicsRoot { get; set; } = null!;

    public List<Page> Mechanics { get; set; } = null!;

    public Page DevelopmentRoot { get; set; } = null!;

    public List<Page> DevelopmentPages { get; set; } = null!;

    public void ApplyTranslations()
    {
    }

    public void Check(string name)
    {
        OrganellesRoot.Check(name);
        StagesRoot.Check(name);
        MechanicsRoot.Check(name);
        DevelopmentRoot.Check(name);

        Organelles.ForEach(p => p.Check(name));
        Stages.ForEach(p => p.Check(name));
        Mechanics.ForEach(p => p.Check(name));
        DevelopmentPages.ForEach(p => p.Check(name));
    }

    public class Page
    {
        public string Name { get; set; } = null!;

        public string InternalName { get; set; } = null!;

        public string Url { get; set; } = null!;

        public List<Section> Sections { get; set; } = null!;

        public List<InfoboxField> InfoboxData { get; set; } = new();

        public string? NoticeSceneName { get; set; }

        public Stage[]? RestrictedToStages { get; set; }

        public void Check(string name)
        {
            if (string.IsNullOrEmpty(InternalName))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Page has no internal name");
            }

            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no name");
            }

            if (string.IsNullOrEmpty(Url))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no URL");
            }

            if (Sections.Count < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has no sections");
            }

            if (Sections.Any(s => string.IsNullOrEmpty(s.SectionBody)))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Page {InternalName} has an empty section");
            }
        }

        public class Section
        {
            public string? SectionHeading { get; set; }

            public string SectionBody { get; set; } = null!;
        }
    }

    public class InfoboxField
    {
        public string Name { get; set; } = null!;

        public string DisplayedValue { get; set; } = null!;
    }
}
