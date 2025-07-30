using System;
using Godot;

/// <summary>
///   A popup that appears on screen to show achievement progress or unlock of an achievement
/// </summary>
public partial class AchievementPopup : PanelContainer
{
    private readonly StringName backgroundStyleName = new("panel");
    private readonly NodePath positionName = new("position");

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

    public void UpdateDataFrom(IAchievement achievement, AchievementStatStore statStore)
    {
        title.Text = achievement.Name.ToString();

        if (achievement.Achieved)
        {
            // TODO: achievement icon

            description.Text = achievement.Description.ToString();

            backgroundToAdjustStyle.AddThemeStyleboxOverride(backgroundStyleName, unlockedStyle);
        }
        else
        {
            icon.Texture = lockedIcon;

            description.Text = achievement.GetProgress(statStore);

            backgroundToAdjustStyle.AddThemeStyleboxOverride(backgroundStyleName, lockedStyle);
        }
    }

    public void PlayAnimation(double duration)
    {
        var animationDuration = 0.6;

        if (animationDuration * 3 > duration)
            throw new ArgumentException("given duration to show achievement animation is too short");

        // Apply initial state
        Visible = true;
        var screenSize = GetViewportRect().Size;

        var offscreenPosition = new Vector2(screenSize.X - Size.X, screenSize.Y);
        var targetPosition = new Vector2(offscreenPosition.X, screenSize.Y - Size.Y);

        Position = offscreenPosition;

        // Setup an animation
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.InOut);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);

        tween.TweenProperty(this, positionName, targetPosition, animationDuration);
        tween.TweenInterval(duration - animationDuration * 2);

        tween.TweenProperty(this, positionName, offscreenPosition, animationDuration);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            backgroundStyleName.Dispose();
            positionName.Dispose();
        }

        base.Dispose(disposing);
    }
}
