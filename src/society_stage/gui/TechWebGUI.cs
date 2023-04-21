using System;
using Godot;

/// <summary>
///   A display of <see cref="TechWeb"/> status and available technologies to select something to research
/// </summary>
public class TechWebGUI : HBoxContainer
{
    [Export]
    public NodePath? TechnologyNameLabelPath;

    [Export]
    public NodePath ResearchButtonPath = null!;

#pragma warning disable CA2213
    private Label technologyNameLabel = null!;
    private Button researchButton = null!;
#pragma warning restore CA2213

    private TechWeb? techWeb;
    private Technology? selectedTechnology;

    [Signal]
    public delegate void OnTechnologyToResearchSelected(string technology);

    public override void _Ready()
    {
        technologyNameLabel = GetNode<Label>(TechnologyNameLabelPath);
        researchButton = GetNode<Button>(ResearchButtonPath);
    }

    /// <summary>
    ///   Shows the technologies in the given tech web
    /// </summary>
    /// <param name="availableTechnologies">The technology data to show</param>
    public void DisplayTechnologies(TechWeb availableTechnologies)
    {
        techWeb = availableTechnologies;

        throw new System.NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TechnologyNameLabelPath != null)
            {
                TechnologyNameLabelPath.Dispose();
                ResearchButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnTechnologySelected(string internalName)
    {
        selectedTechnology = SimulationParameters.Instance.GetTechnology(internalName);
        ShowTechnologyDetails();

        // TODO: allow clearing the selected technology somehow?
    }

    private void ShowTechnologyDetails()
    {
        if (selectedTechnology == null)
        {
            technologyNameLabel.Text = TranslationServer.Translate("SELECT_A_TECHNOLOGY");
            researchButton.Disabled = true;
            return;
        }

        if (techWeb == null)
            throw new InvalidOperationException("TechWeb not set");

        // TODO: query the tech web for if the technology can be researched (pre-requisites fulfilled)
        researchButton.Disabled = techWeb.HasTechnology(selectedTechnology);
    }

    private void OnStartResearch()
    {
        if (selectedTechnology == null)
            return;

        // TODO: warning popup if a previous technology is started and (much) progress would be lost

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnTechnologyToResearchSelected), selectedTechnology.InternalName);
    }
}
