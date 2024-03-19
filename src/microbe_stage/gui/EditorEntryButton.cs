using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
#pragma warning disable CA2213
    [Export]
    private TextureRect highlight = null!;

    [Export]
    private AnimationPlayer buttonAnimationPlayer = null!;

    [Export]
    private TextureRect ammoniaIcon = null!;

    [Export]
    private TextureProgressBar ammoniaReproductionBar = null!;

    [Export]
    private TextureRect phosphateIcon = null!;

    [Export]
    private TextureProgressBar phosphateReproductionBar = null!;

    [Export]
    private PointLight2D editorButtonFlash = null!;
#pragma warning restore CA2213
    private void SetEditorButtonFlashEffect(bool enabled)
    {
        editorButtonFlash.Visible = enabled;
    }

    private void UpdateReproductionProgressBars(float fractionOfAmmonia, float fractionOfPhosphates,
        Texture2D ammoniaBW, Texture2D phosphatesBW)
    {
        ammoniaReproductionBar.Value = fractionOfAmmonia * ammoniaReproductionBar.MaxValue;
        phosphateReproductionBar.Value = fractionOfPhosphates * phosphateReproductionBar.MaxValue;

        if (fractionOfAmmonia >= 1.0f)
        {
            ammoniaReproductionBar.TintProgress = new Color(1, 1, 1, 1);
            ammoniaIcon.Texture = ammoniaBW;
        }

        if (fractionOfPhosphates >= 1.0f)
        {
            phosphateReproductionBar.TintProgress = new Color(1, 1, 1, 1);
            phosphateIcon.Texture = phosphatesBW;
        }
    }

    private void UpdateReproductionProgress(float newAmmoniaValue, float newPhosphateValue)
    {
        ammoniaReproductionBar.Value = newAmmoniaValue;
        phosphateReproductionBar.Value = newPhosphateValue;
    }

    private void OnEditorButtonMouseEnter()
    {
        if (Disabled)
            return;

        highlight.Hide();
        buttonAnimationPlayer.Stop();
    }

    private void OnEditorButtonMouseExit()
    {
        if (Disabled)
            return;

        highlight.Show();
        buttonAnimationPlayer.Play();
    }
}
