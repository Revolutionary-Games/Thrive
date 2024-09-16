﻿using ThriveScriptsShared;

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
        public Page(string name, string internalName, string url, List<Section> sections,
            List<InfoboxField>? infoboxData = null, string? noticeSceneName = null,
            Stage[]? restrictedToStages = null)
        {
            Name = name;
            InternalName = internalName;
            Url = url;
            Sections = sections;
            InfoboxData = infoboxData ?? new List<InfoboxField>();
            NoticeSceneName = noticeSceneName;
            RestrictedToStages = restrictedToStages;
        }

        public string Name { get; set; }

        public string InternalName { get; set; }

        public string Url { get; set; }

        public List<Section> Sections { get; set; }

        public List<InfoboxField> InfoboxData { get; set; }

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

        public string GetInfoBoxData(string key)
        {
            foreach (var infoboxField in InfoboxData)
            {
                if (infoboxField.Name == key)
                    return infoboxField.DisplayedValue;
            }

            throw new KeyNotFoundException("Infobox field by name not found: " + key);
        }

        public class Section
        {
            public Section(string? sectionHeading, string sectionBody)
            {
                SectionHeading = sectionHeading;
                SectionBody = sectionBody;
            }

            public string? SectionHeading { get; set; }

            public string SectionBody { get; set; }
        }
    }

    public class InfoboxField
    {
        public InfoboxField(string name, string displayedValue)
        {
            Name = name;
            DisplayedValue = displayedValue;
        }

        public string Name { get; set; }

        public string DisplayedValue { get; set; }
    }
}
