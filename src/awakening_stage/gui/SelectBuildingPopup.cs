using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;

/// <summary>
///   Allows selecting a structure type from a list of available ones
/// </summary>
public class SelectBuildingPopup : Control
{
    [Export]
    public NodePath? PopupPath;

    [Export]
    public NodePath ButtonsContainerPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    private readonly List<StructureDefinition> validDefinitions = new();

    // Cached string builders used to generate the structure button labels
    private readonly StringBuilder stringBuilder = new();
    private readonly StringBuilder stringBuilder2 = new();

#pragma warning disable CA2213
    private CustomDialog popup = null!;
    private Container buttonsContainer = null!;
    private Button cancelButton = null!;

    private PackedScene richTextScene = null!;
#pragma warning restore CA2213

    private IStructureSelectionReceiver? receiver;

    public override void _Ready()
    {
        popup = GetNode<CustomDialog>(PopupPath);
        buttonsContainer = GetNode<Container>(ButtonsContainerPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        richTextScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        // This is invisible in the editor to make it nicer to edit things
        Visible = true;
    }

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
        IStructureSelectionReceiver selectionReceiver, IAggregateResourceSource availableResources)
    {
        validDefinitions.Clear();
        validDefinitions.AddRange(availableStructures);
        receiver = selectionReceiver;

        var allResources = availableResources.CalculateWholeAvailableResources();

        // Update the structure buttons
        // TODO: cache buttons we can reuse
        buttonsContainer.QueueFreeChildren();

        // createdButtons.Clear();

        // TODO: add a selection wheel as an alternative for more sane controller input
        Control? firstButton = null;

        foreach (var availableStructure in validDefinitions)
        {
            var structureContent = new HBoxContainer
            {
                SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
            };

            // TODO: adjust the button visuals / make the text clickable like for a crafting recipe selection
            var button = new Button
            {
                SizeFlagsHorizontal = 0,
                SizeFlagsVertical = (int)SizeFlags.ShrinkCenter,
                Icon = availableStructure.Icon,
                IconAlign = Button.TextAlign.Center,
                ExpandIcon = true,
                RectMinSize = new Vector2(42, 42),
            };

            structureContent.AddChild(button);
            structureContent.AddChild(new Control
            {
                RectMinSize = new Vector2(5, 0),
            });

            var richText = richTextScene.Instance<CustomRichTextLabel>();
            richText.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            structureContent.AddChild(richText);

            var createdButtonHolder = new CreatedButton(availableStructure, button, richText);

            // createdButtons.Add(availableStructure, createdButtonHolder);
            createdButtonHolder.UpdateResourceCost(allResources, stringBuilder, stringBuilder2);
            buttonsContainer.AddChild(structureContent);

            if (!createdButtonHolder.Disabled)
            {
                var binds = new Array();
                binds.Add(availableStructure.InternalName);
                button.Connect("pressed", this, nameof(OnStructureSelected), binds);

                firstButton ??= button;
            }
        }

        // TODO: sort the buttons based on some criteria

        popup.PopupCenteredShrink();

        if (firstButton == null)
        {
            cancelButton.GrabFocusInOpeningPopup();
        }
        else
        {
            firstButton.GrabFocusInOpeningPopup();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PopupPath != null)
            {
                PopupPath.Dispose();
                ButtonsContainerPath.Dispose();
                CancelButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnStructureSelected(string internalName)
    {
        var selected = validDefinitions.FirstOrDefault(d => d.InternalName == internalName);

        if (selected == null)
        {
            GD.PrintErr($"Invalid structure selected: {internalName}");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();
        popup.Visible = false;

        if (receiver == null)
        {
            GD.PrintErr("No structure receiver set");
            return;
        }

        receiver.OnStructureTypeSelected(selected);
    }

    private class CreatedButton
    {
        private readonly StructureDefinition structureDefinition;
        private readonly Button nativeNode;
        private readonly CustomRichTextLabel customRichTextLabel;

        public CreatedButton(StructureDefinition structureDefinition, Button nativeNode,
            CustomRichTextLabel customRichTextLabel)
        {
            this.structureDefinition = structureDefinition;
            this.nativeNode = nativeNode;
            this.customRichTextLabel = customRichTextLabel;
        }

        public bool Disabled
        {
            get => nativeNode.Disabled;
            private set => nativeNode.Disabled = value;
        }

        public void UpdateResourceCost(System.Collections.Generic.Dictionary<WorldResource, int> allResources,
            StringBuilder stringBuilder,
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
