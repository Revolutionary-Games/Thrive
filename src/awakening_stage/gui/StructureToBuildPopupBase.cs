using System;
using System.Text;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Base type for all menus where selecting a building / structure to be built can be done
/// </summary>
/// <typeparam name="TSelection">The type of object this is allowing selecting from</typeparam>
[GodotAbstract]
public partial class StructureToBuildPopupBase<TSelection> : Control
{
    [Export]
    public NodePath? PopupPath;

    [Export]
    public NodePath ButtonsContainerPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    // Cached string builders used to generate the structure button labels
    protected readonly StringBuilder stringBuilder = new();
    protected readonly StringBuilder stringBuilder2 = new();

#pragma warning disable CA2213
    protected CustomWindow popup = null!;
    protected Container buttonsContainer = null!;
    protected Button cancelButton = null!;

    protected PackedScene richTextScene = null!;
#pragma warning restore CA2213

    protected IStructureSelectionReceiver<TSelection>? receiver;

    protected StructureToBuildPopupBase()
    {
    }

    public override void _Ready()
    {
        popup = GetNode<CustomWindow>(PopupPath);
        buttonsContainer = GetNode<Container>(ButtonsContainerPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        richTextScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        // This is invisible in the editor to make it nicer to edit things
        Visible = true;
    }

    public void Close()
    {
        popup.Close();
    }

    protected void ForwardSelectionToReceiver(TSelection selected)
    {
        GUICommon.Instance.PlayButtonPressSound();
        popup.Visible = false;

        if (receiver == null)
        {
            GD.PrintErr("No structure receiver set");
            return;
        }

        receiver.OnStructureTypeSelected(selected);
    }

    protected void OpenWithButtonSelected(Control? firstButton)
    {
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

    protected (HBoxContainer StructureContent, Button Button, CustomRichTextLabel RichText) CreateStructureSelectionGUI(
        Texture2D icon)
    {
        var structureContent = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

        // TODO: adjust the button visuals / make the text clickable like for a crafting recipe selection
        var button = new Button
        {
            SizeFlagsHorizontal = 0,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Icon = icon,
            IconAlignment = HorizontalAlignment.Center,
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(42, 42),
        };

        structureContent.AddChild(button);
        structureContent.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(5, 0),
        });

        var richText = richTextScene.Instantiate<CustomRichTextLabel>();
        richText.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        structureContent.AddChild(richText);
        return (structureContent, button, richText);
    }

    protected void HandleAddingStructureSelector(HBoxContainer structureContent, bool registerPress,
        Action callback, Button button, ref Control? firstButton)
    {
        buttonsContainer.AddChild(structureContent);

        if (registerPress)
        {
            button.Connect(BaseButton.SignalName.Pressed, Callable.From(callback));

            firstButton ??= button;
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

    protected abstract class CreatedButtonBase
    {
        protected readonly Button nativeNode;
        protected readonly CustomRichTextLabel customRichTextLabel;

        protected CreatedButtonBase(Button nativeNode, CustomRichTextLabel customRichTextLabel)
        {
            this.nativeNode = nativeNode;
            this.customRichTextLabel = customRichTextLabel;
        }

        public bool Disabled
        {
            get => nativeNode.Disabled;
            protected set => nativeNode.Disabled = value;
        }
    }
}
