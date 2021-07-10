using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A group of controls. Controls are shown by group in the key rebind menu
/// </summary>
public class NamedInputGroup : IRegistryType
{
#pragma warning disable 169 // Used through reflection
    private string untranslatedGroupName;
#pragma warning restore 169

    [TranslateFrom("untranslatedGroupName")]
    public string GroupName { get; set; }

    public IReadOnlyList<string> EnvironmentId { get; set; }
    public IReadOnlyList<NamedInputAction> Actions { get; set; }

    [JsonIgnore]
    public string InternalName { get; set; }

    public void Check(string name)
    {
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
