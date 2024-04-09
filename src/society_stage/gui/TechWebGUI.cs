using System;
using System.ComponentModel;
using System.Text;
using Godot;

/// <summary>
///   A display of <see cref="TechWeb"/> status and available technologies to select something to research
/// </summary>
public partial class TechWebGUI : HBoxContainer
{
    [Export]
    public NodePath? TechnologyNameLabelPath;

    [Export]
    public NodePath SelectedTechnologyDescriptionLabelPath = null!;

    [Export]
    public NodePath ResearchButtonPath = null!;

    [Export]
    public NodePath TechNodesContainerPath = null!;

    private readonly StringBuilder descriptionBuilder = new();

#pragma warning disable CA2213
    private Label technologyNameLabel = null!;
    private CustomRichTextLabel selectedTechnologyDescriptionLabel = null!;
    private Button researchButton = null!;

    private Control techNodesContainer = null!;
#pragma warning restore CA2213

    private TechWeb? techWeb;
    private Technology? selectedTechnology;

    [Signal]
    public delegate void OnTechnologyToResearchSelectedEventHandler(string technology);

    public override void _Ready()
    {
        technologyNameLabel = GetNode<Label>(TechnologyNameLabelPath);
        selectedTechnologyDescriptionLabel = GetNode<CustomRichTextLabel>(SelectedTechnologyDescriptionLabelPath);
        researchButton = GetNode<Button>(ResearchButtonPath);
        techNodesContainer = GetNode<Control>(TechNodesContainerPath);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            ShowTechnologyDetails();
        }
    }

    /// <summary>
    ///   Shows the technologies in the given tech web
    /// </summary>
    /// <param name="availableTechnologies">The technology data to show</param>
    public void DisplayTechnologies(TechWeb availableTechnologies)
    {
        techWeb = availableTechnologies;

        // TODO: proper technology display nodes that differentiate between researched and available technologies
        int y = 1;

        // TODO: preserve existing nodes that are still good / update their state
        // this will be needed to make controller focus navigation at least a bit usable
        techNodesContainer.QueueFreeChildren();

        foreach (var technology in SimulationParameters.Instance.GetTechnologies())
        {
            var button = new Button
            {
                Text = technology.Name,
                Disabled = techWeb.HasTechnology(technology),
            };

            // TODO: temporary positioning logic
            if (technology == SimulationParameters.Instance.GetTechnology("steamPower"))
            {
                button.Position = new Vector2(250, 250);
            }
            else
            {
                button.Position = new Vector2(25, 65 * y);
                ++y;
            }

            techNodesContainer.AddChild(button);

            button.Connect(BaseButton.SignalName.Pressed, Callable.From(() => OnTechnologySelected(technology)));

            // TODO: ensure the container is large enough min size to contain everything
        }

        // TODO: layout the technologies in a sensible way (we could have a tool to precalculate a good layout)

        // TODO: draw lines connecting technologies
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TechnologyNameLabelPath != null)
            {
                TechnologyNameLabelPath.Dispose();
                SelectedTechnologyDescriptionLabelPath.Dispose();
                ResearchButtonPath.Dispose();
                TechNodesContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnTechnologySelected(Technology technology)
    {
        selectedTechnology = technology;
        ShowTechnologyDetails();

        // TODO: allow clearing the selected technology somehow?
    }

    private void ShowTechnologyDetails()
    {
        if (selectedTechnology == null)
        {
            technologyNameLabel.Text = Localization.Translate("SELECT_A_TECHNOLOGY");
            selectedTechnologyDescriptionLabel.Text = null;
            researchButton.Disabled = true;
            return;
        }

        if (techWeb == null)
            throw new InvalidOperationException("TechWeb not set");

        technologyNameLabel.Text = selectedTechnology.Name;

        // TODO: different sized fonts for the different sections

        // TODO: show in red if the player doesn't have the research capability
        descriptionBuilder.Append(Localization.Translate("TECHNOLOGY_REQUIRED_LEVEL")
            .FormatSafe(Localization.Translate(selectedTechnology.RequiresResearchLevel
                .GetAttribute<DescriptionAttribute>().Description)));

        // TODO: a quick description for a technology

        // TODO: display all the data about the technology

        selectedTechnologyDescriptionLabel.Text = descriptionBuilder.ToString();
        descriptionBuilder.Clear();

        // TODO: query the tech web for if the technology can be researched (pre-requisites fulfilled)
        researchButton.Disabled = techWeb.HasTechnology(selectedTechnology);

        // TODO: disable the research button if the tech is currently being researched
    }

    private void OnStartResearch()
    {
        if (selectedTechnology == null)
            return;

        // TODO: warning popup if a previous technology is started and (much) progress would be lost

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnTechnologyToResearchSelected, selectedTechnology.InternalName);

        // Disable the button to disallow trying to start the same research multiple times (we don't know if our signal
        // could fail)
        researchButton.Disabled = true;
    }
}
