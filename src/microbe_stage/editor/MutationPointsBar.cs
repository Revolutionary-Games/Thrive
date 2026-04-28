using System;
using Godot;

/// <summary>
///   Mutation points bar that shows the remaining mutation points in the editor
/// </summary>
public partial class MutationPointsBar : HBoxContainer
{
    [Export]
    public bool ShowPercentageSymbol = true;

    private const string PercentageValuePlaceholder = "{0}";

#pragma warning disable CA2213
    [Export]
    private Label currentMutationPointsLabel = null!;

    [Export]
    private TextureRect mutationPointsArrow = null!;

    [Export]
    private Label resultingMutationPointsLabel = null!;

    [Export]
    private Label baseMutationPointsLabel = null!;

    [Export]
    private ProgressBar mutationPointsBar = null!;

    [Export]
    private ProgressBar mutationPointsSubtractBar = null!;

    [Export]
    private AnimationPlayer animationPlayer = null!;
#pragma warning restore CA2213

    private string freebuildingText = string.Empty;
    private string percentagePrefix = string.Empty;
    private string percentageSuffix = " %";
    private bool hasMutationPointDisplayState;
    private bool lastFreebuilding;
    private bool lastShowResultingPoints;
    private double lastCurrentMutationPoints;
    private double lastPossibleMutationPoints;

    public override void _Ready()
    {
        UpdatePercentageFormatParts();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what != NotificationTranslationChanged)
            return;

        UpdatePercentageFormatParts();

        if (hasMutationPointDisplayState)
        {
            UpdateMutationPoints(lastFreebuilding, lastShowResultingPoints, lastCurrentMutationPoints,
                lastPossibleMutationPoints);
        }
    }

    public void UpdateBar(double currentMutationPoints, double possibleMutationPoints, bool tween = true)
    {
        if (tween)
        {
            GUICommon.Instance.TweenBarValue(mutationPointsBar, possibleMutationPoints, Constants.BASE_MUTATION_POINTS,
                0.5f);
            GUICommon.Instance.TweenBarValue(mutationPointsSubtractBar, currentMutationPoints,
                Constants.BASE_MUTATION_POINTS, 0.7f);
        }
        else
        {
            mutationPointsBar.Value = possibleMutationPoints;
            mutationPointsBar.MaxValue = Constants.BASE_MUTATION_POINTS;
            mutationPointsSubtractBar.Value = currentMutationPoints;
            mutationPointsSubtractBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        }

        mutationPointsSubtractBar.SelfModulate = possibleMutationPoints < 0 ?
            new Color(0.72f, 0.19f, 0.19f) :
            new Color(0.72f, 0.72f, 0.72f);
    }

    public void UpdateMutationPoints(bool freebuilding, bool showResultingPoints, double currentMutationPoints,
        double possibleMutationPoints)
    {
        hasMutationPointDisplayState = true;
        lastFreebuilding = freebuilding;
        lastShowResultingPoints = showResultingPoints;
        lastCurrentMutationPoints = currentMutationPoints;
        lastPossibleMutationPoints = possibleMutationPoints;

        // Make sure tiny negative values aren't shown improperly
        if (currentMutationPoints < 0 && currentMutationPoints > Constants.ALLOWED_MP_OVERSHOOT)
            currentMutationPoints = 0;

        if (freebuilding)
        {
            mutationPointsArrow.Hide();
            resultingMutationPointsLabel.Hide();
            baseMutationPointsLabel.Hide();

            currentMutationPointsLabel.Text = freebuildingText;
        }
        else
        {
            if (showResultingPoints)
            {
                mutationPointsArrow.Show();
                resultingMutationPointsLabel.Show();

                currentMutationPointsLabel.Text = $"({percentagePrefix}{currentMutationPoints:0.#}";
                resultingMutationPointsLabel.Text = $"{percentagePrefix}{possibleMutationPoints:F0})";
            }
            else
            {
                mutationPointsArrow.Hide();
                resultingMutationPointsLabel.Hide();

                currentMutationPointsLabel.Text = $"{percentagePrefix}{currentMutationPoints:0.#}";
            }

            baseMutationPointsLabel.Text = $"/ {percentagePrefix}{Constants.BASE_MUTATION_POINTS:F0}{percentageSuffix}";
        }
    }

    public void PlayFlashAnimation()
    {
        animationPlayer.Play("FlashBar");
    }

    private void UpdatePercentageFormatParts()
    {
        freebuildingText = Localization.Translate("FREEBUILDING");

        if (!ShowPercentageSymbol)
        {
            percentagePrefix = string.Empty;
            percentageSuffix = string.Empty;
            return;
        }

        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");
        var placeholderPosition = percentageFormat.IndexOf(PercentageValuePlaceholder, StringComparison.Ordinal);

        if (placeholderPosition < 0)
        {
            percentagePrefix = string.Empty;
            percentageSuffix = " %";
            return;
        }

        percentagePrefix = percentageFormat[..placeholderPosition];
        percentageSuffix = percentageFormat[(placeholderPosition + PercentageValuePlaceholder.Length)..];
    }
}
