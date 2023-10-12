using System.Collections.Generic;
using Newtonsoft.Json;

public class GameWiki : IRegistryType
{
    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void ApplyTranslations()
    {
    }

    public void Check(string name)
    {
    }

    [JsonProperty]
    public Page OrganellesRoot { get; set; } = null!;

    [JsonProperty]
    public List<Page> Organelles { get; set; } = null!;

    public class Page
    {
        public string Name { get; set; } = null!;

        public string InternalName { get; set; } = null!;

        public string Url { get; set; } = null!;

        [JsonProperty]
        public List<Section> Sections { get; set; } = null!;

        public class Section
        {
            public string? SectionHeading { get; set; }

            public string SectionBody { get; set; } = null!;
        }
    }
}