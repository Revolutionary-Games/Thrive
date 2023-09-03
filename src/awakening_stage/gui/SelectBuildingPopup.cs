using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Allows selecting a structure type from a list of available ones
/// </summary>
public class SelectBuildingPopup : StructureToBuildPopupBase<StructureDefinition>
{
    private readonly List<StructureDefinition> validDefinitions = new();

    /// <summary>
    ///   Opens this popup to allow selecting from the available structures
    /// </summary>
    /// <param name="availableStructures">Which structures to show as available</param>
    /// <param name="selectionReceiver">
    ///   Object receiving the selected structure. This is used instead of signals to allow passing a C# object that is
    ///   not necessarily a Godot object.
    /// </param>
    /// <param name="availableResources">Available resources to determine which structures are buildable</param>
    public void OpenWithStructures(IEnumerable<StructureDefinition> availableStructures,
        IStructureSelectionReceiver<StructureDefinition> selectionReceiver, IAggregateResourceSource availableResources)
    {
        validDefinitions.Clear();
        validDefinitions.AddRange(availableStructures);
        receiver = selectionReceiver;

        var allResources = availableResources.CalculateWholeAvailableResources();

        // Update the structure buttons
        // TODO: cache buttons we can reuse
        buttonsContainer.QueueFreeChildren();

        // TODO: add a selection wheel as an alternative for more sane controller input
        Control? firstButton = null;

        foreach (var availableStructure in validDefinitions)
        {
            var (structureContent, button, richText) = CreateStructureSelectionGUI(availableStructure.Icon);

            var createdButtonHolder = new CreatedButton(availableStructure, button, richText);

            // createdButtons.Add(availableStructure, createdButtonHolder);
            createdButtonHolder.UpdateResourceCost(allResources, stringBuilder, stringBuilder2);

            HandleAddingStructureSelector(structureContent, !createdButtonHolder.Disabled, nameof(OnStructureSelected),
                availableStructure.InternalName, button, ref firstButton);
        }

        // TODO: sort the buttons based on some criteria

        OpenWithButtonSelected(firstButton);
    }

    private void OnStructureSelected(string internalName)
    {
        var selected = validDefinitions.FirstOrDefault(d => d.InternalName == internalName);

        if (selected == null)
        {
            GD.PrintErr($"Invalid structure selected: {internalName}");
            return;
        }

        ForwardSelectionToReceiver(selected);
    }

    private class CreatedButton : CreatedButtonBase
    {
        private readonly StructureDefinition structureDefinition;

        public CreatedButton(StructureDefinition structureDefinition, Button nativeNode,
            CustomRichTextLabel customRichTextLabel) : base(nativeNode, customRichTextLabel)
        {
            this.structureDefinition = structureDefinition;
        }

        public void UpdateResourceCost(IReadOnlyDictionary<WorldResource, int> allResources,
            StringBuilder stringBuilder, StringBuilder stringBuilder2)
        {
            // Disabled if can't start the building
            bool canStart = structureDefinition.CanStart(allResources) == null;
            Disabled = !canStart;

            stringBuilder.Clear();
            stringBuilder2.Clear();

            ResourceAmountHelpers.CreateRichTextForResourceAmounts(structureDefinition.ScaffoldingCost, allResources,
                stringBuilder);

            ResourceAmountHelpers.CreateRichTextForResourceAmounts(structureDefinition.TotalCost, allResources,
                stringBuilder2);

            if (!canStart)
            {
                customRichTextLabel.ExtendedBbcode = TranslationServer.Translate(
                        "STRUCTURE_SELECTION_MENU_ENTRY_NOT_ENOUGH_RESOURCES")
                    .FormatSafe(structureDefinition.Name, stringBuilder.ToString(), stringBuilder2.ToString());
            }
            else
            {
                customRichTextLabel.ExtendedBbcode = TranslationServer.Translate("STRUCTURE_SELECTION_MENU_ENTRY")
                    .FormatSafe(structureDefinition.Name, stringBuilder.ToString(), stringBuilder2.ToString());
            }
        }
    }
}
