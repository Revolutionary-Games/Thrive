using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows a gallery of the achievements the player has / can get
/// </summary>
public partial class AchievementsGallery : Control
{
    private readonly List<AchievementCard> achievementCards = new();

#pragma warning disable CA2213
    [Export]
    private Container cardContainer = null!;

    [Export]
    private FocusGrabber grabberToUpdate = null!;

    private PackedScene cardScene = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        base._Ready();

        cardScene = GD.Load<PackedScene>("res://src/general/achievements/AchievementCard.tscn");
    }

    public void Refresh()
    {
        // Refresh the cards

        // TODO: could do a lighter refresh than this, but for now the performance of this is sufficient with the
        // achievement count we have currently
        foreach (var achievementCard in achievementCards)
        {
            achievementCard.QueueFree();
        }

        achievementCards.Clear();

        bool first = true;

        foreach (var achievement in AchievementsManager.Instance.GetAchievements())
        {
            // TODO: should this show a card that just has the text "hidden" on it?
            if (achievement.HideIfNotAchieved && !achievement.Achieved)
                continue;

            var instance = cardScene.Instantiate<AchievementCard>();

            instance.UpdateDataFrom(achievement, AchievementsManager.Instance.GetStats());

            cardContainer.AddChild(instance);
            achievementCards.Add(instance);

            if (first)
            {
                first = false;
                grabberToUpdate.NodeToGiveFocusTo = instance.GetPath();
            }
        }
    }
}
