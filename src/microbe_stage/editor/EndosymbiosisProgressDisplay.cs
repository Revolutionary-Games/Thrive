using Godot;

/// <summary>
///   Shows the progress of an endosymbiosis process
/// </summary>
public partial class EndosymbiosisProgressDisplay : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Label speciesNameLabel = null!;

    [Export]
    private SpeciesPreview speciesPreview = null!;

    [Export]
    private ProgressBar progressBar = null!;

    [Export]
    private Button finishButton = null!;
#pragma warning restore CA2213

    private Species? species;

    [Signal]
    public delegate void OnFinishedEventHandler(int speciesId);

    [Signal]
    public delegate void OnCancelledEventHandler(int speciesId);

    public void SetSpecies(Species targetSpecies)
    {
        species = targetSpecies;

        speciesNameLabel.Text = species.FormattedName;
        speciesPreview.PreviewSpecies = species;
    }

    public void UpdateProgress(int requiredCount, int currentCount, bool isComplete)
    {
        progressBar.MaxValue = requiredCount;
        progressBar.Value = currentCount;

        finishButton.Disabled = !isComplete;
    }

    private void OnFinish()
    {
        if (species == null)
        {
            GD.PrintErr("Species not set but should be already");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnFinished, species.ID);
    }

    private void OnCancel()
    {
        if (species == null)
        {
            GD.PrintErr("Species not set but should be already");
            return;
        }

        // TODO: could maybe in the future show a warning before cancelling and losing progress potentially?

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnCancelled, species.ID);
    }
}
