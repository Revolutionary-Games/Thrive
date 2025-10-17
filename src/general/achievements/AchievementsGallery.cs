using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows a gallery of the achievements the player has / can get
/// </summary>
public partial class AchievementsGallery : Control
{
    private readonly Dictionary<int, AchievementCard> achievementCards = new();

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

        // Add all achievement cards

        bool first = true;

        foreach (var achievement in AchievementsManager.Instance.GetAchievements())
        {
            var instance = cardScene.Instantiate<AchievementCard>();
            instance.Visible = false;

            cardContainer.AddChild(instance);
            achievementCards.Add(achievement.Identifier, instance);

            if (first)
            {
                first = false;
                grabberToUpdate.NodeToGiveFocusTo = instance.GetPath();
            }
        }
    }

    public void Refresh()
    {
        // Refresh the cards

        foreach (var achievement in AchievementsManager.Instance.GetAchievements())
        {
            // TODO: should this show a card that just has the text "hidden" on it?
            if (achievement.HideIfNotAchieved && !achievement.Achieved)
                continue;

            if (achievementCards.TryGetValue(achievement.Identifier, out var achievementCard))
            {
                achievementCard.UpdateDataFrom(achievement, AchievementsManager.Instance.GetStats());
                achievementCard.Visible = true;
            }
        }
    }
}
