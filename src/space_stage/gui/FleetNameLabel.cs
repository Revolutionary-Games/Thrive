using System;
using Godot;
using Newtonsoft.Json;

public class FleetNameLabel : Button, IEntityNameLabel
{
    private string translationTemplate = null!;

    private string? previousName;
    private float previousStrength;

    public FleetNameLabel()
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
            case SpaceFleet fleet:

                if (fleet.FleetName == previousName &&
                    Math.Abs(fleet.CombatPower - previousStrength) < MathUtils.EPSILON)
                {
                    return;
                }

                previousName = fleet.FleetName;
                previousStrength = fleet.CombatPower;

                newText = translationTemplate.FormatSafe(previousName, previousStrength);
                break;

            default:
                throw new ArgumentException("Unsupported entity type", nameof(entity));
        }

        Text = newText;
    }

    private void UpdateTranslationTemplate()
    {
        translationTemplate = TranslationServer.Translate("NAME_LABEL_FLEET");
    }

    private void ForwardSelection()
    {
        OnEntitySelectedHandler?.Invoke();
    }
}
