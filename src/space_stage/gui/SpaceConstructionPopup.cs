using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Allows selecting a thing for a space fleet to build
/// </summary>
public partial class SpaceConstructionPopup : StructureToBuildPopupBase<SpaceStructureDefinition>
{
    private readonly List<SpaceStructureDefinition> validDefinitions = new();

    /// <summary>
    ///   Opens this popup to allow selecting from the available space structures
    /// </summary>
    public void OpenWithStructures(IEnumerable<SpaceStructureDefinition> availableStructures,
        IStructureSelectionReceiver<SpaceStructureDefinition> selectionReceiver,
        IResourceContainer availableResources)
    {
        validDefinitions.Clear();
        validDefinitions.AddRange(availableStructures);
        receiver = selectionReceiver;

        // Update the structure buttons
        // TODO: cache buttons we can reuse
        buttonsContainer.QueueFreeChildren();

        // TODO: add a selection wheel as an alternative for more sane controller input
        Control? firstButton = null;

        foreach (var availableStructure in validDefinitions)
        {
            var (structureContent, button, richText) = CreateStructureSelectionGUI(availableStructure.Icon);

            var createdButtonHolder = new CreatedButton(availableStructure, button, richText);

            createdButtonHolder.UpdateResourceCost(availableResources, stringBuilder, stringBuilder2);

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
        private readonly SpaceStructureDefinition structureDefinition;

        public CreatedButton(SpaceStructureDefinition structureDefinition, Button nativeNode,
            CustomRichTextLabel customRichTextLabel) : base(nativeNode, customRichTextLabel)
        {
            this.structureDefinition = structureDefinition;
        }

        public void UpdateResourceCost(IResourceContainer allResources, StringBuilder stringBuilder,
            StringBuilder stringBuilder2)
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
