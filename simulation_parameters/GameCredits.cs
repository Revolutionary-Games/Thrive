using System.Collections.Generic;
using Newtonsoft.Json;

public class GameCredits : IRegistryType
{
    public GameDevelopers Developers { get; set; } = null!;

    /// <summary>
    ///   These are the donations from the wiki. The structure is "year&lt;month, list of donations&gt;"
    /// </summary>
    public Dictionary<string, Dictionary<string, List<string>>> Donations { get; set; } = null!;

    public List<string> Translators { get; set; } = null!;

    public PatronsList Patrons { get; set; } = null!;

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (Developers == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Developers is missing");
        }

        if (Developers.Current == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Current Developers are missing");
        }

        if (Developers.Past == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Past Developers are missing");
        }

        if (Developers.Outside == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Outside contributors are missing");
        }

        if (Donations == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Donations are missing");
        }

        if (Translators == null || Translators.Count < 1)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Translators are missing");
        }

        if (Patrons == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Patrons are missing");
        }

        if (Patrons.VIPPatrons == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Patrons (VIP) are missing");
        }

        if (Patrons.DevBuildPatrons == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Patrons (devbuild) are missing");
        }

        if (Patrons.SupporterPatrons == null)
        {
            throw new InvalidRegistryDataException(nameof(GameCredits), GetType().Name,
                "Patrons (supporter) are missing");
        }
    }

    public void ApplyTranslations()
    {
    }

    public class GameDevelopers
    {
        // These are maps of the team and list of their members
        public Dictionary<string, List<DeveloperPerson>> Current { get; set; } = null!;
        public Dictionary<string, List<DeveloperPerson>> Past { get; set; } = null!;

        /// <summary>
        ///   These are outside contributors (non-team members)
        /// </summary>
        public Dictionary<string, List<DeveloperPerson>> Outside { get; set; } = null!;
    }

    public class DeveloperPerson
    {
        [JsonProperty(PropertyName = "person")]
        public string Name { get; set; } = null!;

        public bool Lead { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PatronsList
    {
        [JsonProperty]
        public List<string> VIPPatrons { get; set; } = null!;

        [JsonProperty]
        public List<string> DevBuildPatrons { get; set; } = null!;

        [JsonProperty]
        public List<string> SupporterPatrons { get; set; } = null!;
    }
}
