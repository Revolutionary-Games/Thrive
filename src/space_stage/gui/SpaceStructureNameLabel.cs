using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Label on a structure in space, can be clicked to select it
/// </summary>
public partial class SpaceStructureNameLabel : Button, IEntityNameLabel
{
    private string translationTemplate = null!;

    public SpaceStructureNameLabel()
    {
        UpdateTranslationTemplate();
    }

    public event IEntityNameLabel.OnEntitySelected? OnEntitySelectedHandler;

    [JsonIgnore]
    public Control LabelControl => this;

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            UpdateTranslationTemplate();
        }
    }

    public void UpdateFromEntity(IEntityWithNameLabel entity)
    {
        string newText;

        switch (entity)
        {
            case PlacedSpaceStructure structure:

                if (structure.Completed)
                {
                    newText = structure.ReadableName;
                }
                else
                {
                    newText = translationTemplate.FormatSafe(structure.Definition.Name);
                }

                break;

            default:
                throw new ArgumentException("Unsupported entity type", nameof(entity));
        }

        Text = newText;
    }

    private void UpdateTranslationTemplate()
    {
        translationTemplate = Localization.Translate("NAME_LABEL_STRUCTURE_UNFINISHED");
    }

    private void ForwardSelection()
    {
        OnEntitySelectedHandler?.Invoke();
    }
}
