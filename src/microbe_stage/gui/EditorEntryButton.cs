using Godot;

/// <summary>
///   Button for entering editor in creature stages
/// </summary>
public partial class EditorEntryButton : TextureButton
{
#pragma warning disable CA2213
    [Export]
    private Texture2D ammoniaBW = null!;

    [Export]
    private Texture2D phosphatesBW = null!;

    [Export]
    private Texture2D ammoniaInv = null!;

    [Export]
    private Texture2D phosphatesInv = null!;

    [Export]
    private Texture2D gameteButtonNormal = null!;

    [Export]
    private Texture2D gameteButtonPressed = null!;

    [Export]
    private Texture2D gameteButtonHover = null!;

    [Export]
    private Texture2D gameteButtonDisabled = null!;

    [Export]
    private Control readyPrompt = null!;

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
    private Control reproductionBar = null!;

    [Export]
    private PointLight2D editorButtonFlash = null!;

    private Texture2D? originalButtonNormal;
    private Texture2D? originalButtonPressed;
    private Texture2D? originalButtonHover;
    private Texture2D? originalButtonDisabled;
#pragma warning restore CA2213

    public void SetEditorButtonFlashEffect(bool enabled)
    {
        editorButtonFlash.Visible = enabled;
    }

    public void UpdateReproductionProgressBars(float fractionOfAmmonia, float fractionOfPhosphates)
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

    public void ShowReproductionDialog()
    {
        Disabled = false;
        highlight.Show();
        phosphateReproductionBar.TintProgress = new Color(1, 1, 1, 1);
        ammoniaReproductionBar.TintProgress = new Color(1, 1, 1, 1);
        phosphateIcon.Texture = phosphatesBW;
        ammoniaIcon.Texture = ammoniaBW;
        buttonAnimationPlayer.Play("EditorButtonFlash");

        readyPrompt.Visible = true;
    }

    public void HideReproductionDialog()
    {
        if (!Disabled)
            Disabled = true;

        highlight.Hide();
        reproductionBar.Show();
        phosphateReproductionBar.TintProgress = new Color(0.69f, 0.42f, 1, 1);
        ammoniaReproductionBar.TintProgress = new Color(1, 0.62f, 0.12f, 1);
        phosphateIcon.Texture = phosphatesInv;
        ammoniaIcon.Texture = ammoniaInv;
        buttonAnimationPlayer.Stop();

        readyPrompt.Visible = false;
    }

    public void SetNormalStyle()
    {
        // If not changed away from a special style, do nothing
        if (originalButtonNormal == null)
            return;

        TextureNormal = originalButtonNormal;
        TexturePressed = originalButtonPressed;
        TextureHover = originalButtonHover;
        TextureDisabled = originalButtonDisabled;
    }

    public void SetGameteStyle()
    {
        RememberNormalStyle();

        TextureNormal = gameteButtonNormal;
        TexturePressed = gameteButtonPressed;
        TextureHover = gameteButtonHover;
        TextureDisabled = gameteButtonDisabled;
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

    private void RememberNormalStyle()
    {
        if (originalButtonNormal != null)
            return;

        originalButtonNormal = TextureNormal;
        originalButtonPressed = TexturePressed;
        originalButtonHover = TextureHover;
        originalButtonDisabled = TextureDisabled;
    }
}
