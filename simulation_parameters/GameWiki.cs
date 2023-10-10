using System.Collections.Generic;

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

    public List<OrganelleWikiPage> Organelles { get; set; } = null!;

    public class OrganelleWikiPage
    {
        public string InternalName { get; set; } = null!;
        public string Url { get; set; } = null!;
        public OrganelleSections Sections { get; set; } = null!;

        public class OrganelleSections
        {
            public string Description { get; set; } = null!;
            public string Requirements { get; set; } = null!;
            public string Processes { get; set; } = null!;
            public string Modifications { get; set; }  = null!;
            public string Effects { get; set; } = null!;
            public string Upgrades { get; set; } = null!;
            public string Strategy { get; set; } = null!;
            public string ScientificBackground { get; set; } = null!;
        }
    }
}