using Godot;

/// <summary>
///   Mutation points bar that shows the remaining mutation points in the editor
/// </summary>
public partial class MutationPointsBar : HBoxContainer
{
    [Export]
    public bool ShowPercentageSymbol = true;

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

    public override void _Ready()
    {
        freebuildingText = Localization.Translate("FREEBUILDING");
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

                currentMutationPointsLabel.Text = $"({currentMutationPoints:0.#}";
                resultingMutationPointsLabel.Text = $"{possibleMutationPoints:F0})";
            }
            else
            {
                mutationPointsArrow.Hide();
                resultingMutationPointsLabel.Hide();

                currentMutationPointsLabel.Text = $"{currentMutationPoints:0.#}";
            }

            if (ShowPercentageSymbol)
            {
                // TODO: switch this class to using translation keys properly instead of this approach
                // We have PERCENTAGE_VALUE translation string, but I didn't add that here as this already uses partial
                // formatting with plain strings so I just extended the existing solution here -hhyyrylainen
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0} %";
            }
            else
            {
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
        }
    }

    public void PlayFlashAnimation()
    {
        animationPlayer.Play("FlashBar");
    }
}
