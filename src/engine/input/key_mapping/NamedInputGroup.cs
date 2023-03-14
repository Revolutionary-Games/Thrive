using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A group of controls. Controls are shown by group in the key rebind menu
/// </summary>
public class NamedInputGroup : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedGroupName;
#pragma warning restore 169,649

    [TranslateFrom(nameof(untranslatedGroupName))]
    public string GroupName { get; set; } = null!;

    public IReadOnlyList<string> EnvironmentId { get; set; } = null!;
    public IReadOnlyList<NamedInputAction> Actions { get; set; } = null!;

    [JsonIgnore]
    public string InternalName { get; set; } = string.Empty;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(GroupName))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "NamedInputGroup has no group name");
        }

        if (EnvironmentId == null || EnvironmentId.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "NamedInputGroup has no environment ids");
        }

        if (Actions == null || Actions.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "NamedInputGroup has no actions");
        }

        InternalName = GroupName;

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        foreach (var action in Actions)
        {
            action.Check(string.Empty);
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);

        foreach (var action in Actions)
        {
            action.ApplyTranslations();
        }
    }
}
