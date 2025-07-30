using Godot;

/// <summary>
///   Handles the display logic of an achievement card
/// </summary>
public partial class AchievementCard : MarginContainer
{
    private readonly StringName backgroundStyleName = new("panel");

#pragma warning disable CA2213
    [Export]
    private Label title = null!;

    [Export]
    private TextureRect icon = null!;

    [Export]
    private Label description = null!;

    [Export]
    private PanelContainer backgroundToAdjustStyle = null!;

    [Export]
    private StyleBox unlockedStyle = null!;

    [Export]
    private StyleBox lockedStyle = null!;

    [Export]
    private Texture2D lockedIcon = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        base._Ready();

        this.RegisterCustomFocusDrawer();
    }

    public void UpdateDataFrom(IAchievement achievement, AchievementStatStore stats)
    {
        title.Text = achievement.Name.ToString();

        if (achievement.Achieved)
        {
            title.Text = Localization.Translate("ACHIEVEMENT_ACHIEVED");

            description.Text = achievement.Name.ToString();

            backgroundToAdjustStyle.AddThemeStyleboxOverride(backgroundStyleName, unlockedStyle);

            // TODO: icon
            // icon.Texture = achievement.
        }
        else
        {
            title.Text = Localization.Translate("ACHIEVEMENT_LOCKED");

            if (achievement.HasAnyProgress(stats))
            {
                description.Text = achievement.GetProgress(stats);
            }
            else
            {
                description.Text = achievement.Description.ToString();
            }

            backgroundToAdjustStyle.AddThemeStyleboxOverride(backgroundStyleName, lockedStyle);

            icon.Texture = lockedIcon;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            backgroundStyleName.Dispose();
        }

        base.Dispose(disposing);
    }
}
