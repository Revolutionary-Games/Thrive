using Godot;

public class MultiplayerGameMode : IRegistryType
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   Index for this game mode in menus
    /// </summary>
    public int Index;

    [TranslateFrom(nameof(untranslatedDescription))]
    public string Description = null!;

    /// <summary>
    ///   A mandatory scene path to a <see cref="IMultiplayerStage"/>.
    /// </summary>
    public string MainScene = null!;

    /// <summary>
    ///   A scene path to a <see cref="IEditor"/> if there's any.
    /// </summary>
    public string? EditorScene;

    public string? SettingsGUI;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
    private string? untranslatedDescription;
#pragma warning restore 169,649

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");
        }

        if (Index < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Index is negative");

        if (string.IsNullOrEmpty(Description))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Description is not set");
        }

        if (string.IsNullOrEmpty(MainScene))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "MainScene is not set");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    // TODO: REMOVE THIS!!
    // A temporary hack until thrive-pybabel's json extractor is updated to include "Description" key to extract.
    private void TempTranslationHack()
    {
        TranslationServer.Translate("MICROBE_ARENA_DESCRIPTION");
    }
}
