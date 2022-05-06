﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Tweaked colour picker allows for tooltip texts translations and has a customized theming.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    /// <summary>
    ///   This is where presets are stored after a colour picker exited scene tree.
    ///   <remarks>
    ///     The Key is the group name; <br />
    ///     The Value is the preset storage.
    ///   </remarks>
    /// </summary>
    private static readonly Dictionary<string, PresetGroupStorage> PresetsStorage = new();

    /// <summary>
    ///   This is the local storage of its preset children
    ///   so that we don't need to call GetChildren() when deleting one.
    /// </summary>
    private readonly List<TweakedColourPickerPreset> presets = new();

    private HSlider sliderROrH = null!;
    private HSlider sliderGOrS = null!;
    private HSlider sliderBOrV = null!;
    private HSlider sliderA = null!;
    private ToolButton pickerButton = null!;
    private CustomCheckBox? hsvCheckBox;
    private CustomCheckBox rawCheckBox = null!;
    private Label htmlColourStart = null!;
    private LineEdit htmlColourEdit = null!;
    private HSeparator separator = null!;
    private GridContainer? presetsContainer;
    private TextureButton? addPresetButton;

    private bool hsvButtonEnabled = true;
    private bool rawButtonEnabled = true;
    private bool presetsEditable = true;
    private bool presetsVisible = true;
    private PickerMode mode;

    private bool pickingColor;
    private Color colorBeforePicking;
    private float pickerTimeElapsed;

    private PresetGroupStorage groupStorage = null!;

    private delegate void AddPresetDelegate(Color colour);

    private delegate void DeletePresetDelegate(Color colour);

    private enum PickerMode
    {
        Rgb,
        Hsv,
        Raw,
    }

    [Export]
    public string PresetGroup { get; private set; } = "default";

    /// <summary>
    ///   Decide if user can toggle HSV CustomCheckBox to switch HSV mode.
    /// </summary>
    [Export]
    public bool HSVButtonEnabled
    {
        get => hsvButtonEnabled;
        set
        {
            hsvButtonEnabled = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Decide if user can toggle raw CustomCheckBox to switch raw mode.
    /// </summary>
    [Export]
    public bool RawButtonEnabled
    {
        get => rawButtonEnabled;
        set
        {
            rawButtonEnabled = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Change the picker's HSV mode.
    ///   <remarks>
    ///     This is not named HSVMode because this hides a Godot property to ensure that
    ///     when switching HSV mode the buttons get properly updated.
    ///   </remarks>
    /// </summary>
    [Export]
    public new bool HsvMode
    {
        get => base.HsvMode;
        set
        {
            mode = value ? PickerMode.Hsv : PickerMode.Rgb;
            base.HsvMode = value;
            UpdateButtonsState();

            // Update HSV sliders' tooltips
            UpdateTooltips();
        }
    }

    /// <summary>
    ///   Change the picker's raw mode.
    /// </summary>
    [Export]
    public new bool RawMode
    {
        get => base.RawMode;
        set
        {
            mode = value ? PickerMode.Raw : PickerMode.Rgb;

            if (value == false)
                ValidateRgbColor();

            base.RawMode = value;
            UpdateButtonsState();
            UpdateTooltips();
        }
    }

    /// <summary>
    ///   Decide if user can edit the presets.
    /// </summary>
    [Export]
    public bool PresetsEditable
    {
        get => presetsEditable;
        set
        {
            presetsEditable = value;

            if (addPresetButton == null)
                return;

            addPresetButton.Disabled = !value;
        }
    }

    /// <summary>
    ///   Decide if the presets and the add preset button is visible.
    ///   Completely hides the native one to avoid hidden native controls reappearing.
    /// </summary>
    [Export]
    public new bool PresetsVisible
    {
        get => presetsVisible;
        set
        {
            presetsVisible = value;

            if (presetsContainer == null)
                return;

            if (addPresetButton == null)
                throw new Exception("Preset button is null even though presets container is initialized already");

            separator.Visible = value;
            presetsContainer.Visible = value;
            addPresetButton.Visible = value;
        }
    }

    /// <summary>
    ///   Change the TweakedColourPicker's current colour.
    ///   <remarks>
    ///     Have to disable warning CA1721 because this hides Godot property Color
    ///     to emit a signal when changing its color. This can't be renamed.
    ///   </remarks>
    /// </summary>
#pragma warning disable CA1721
    public new Color Color
#pragma warning restore CA1721
    {
        get => base.Color;
        set
        {
            if (Color == value)
                return;

            base.Color = value;
            EmitSignal("color_changed", value);
        }
    }

    /// <summary>
    ///   Hide Godot property PresetsEnabled to avoid unexpected changes
    ///   which may cause the hidden native buttons reappear.
    /// </summary>
    private new bool PresetsEnabled { get => PresetsEditable; set => PresetsEditable = value; }

    public override void _Ready()
    {
        base._Ready();

        // Hide replaced native controls. Can't delete them because it will crash Godot.
        var baseControl = GetChild(4);
        baseControl.GetChild<Control>(4).Hide();
        GetChild<Control>(5).Hide();
        GetChild<Control>(6).Hide();
        GetChild<Control>(7).Hide();

        // Get controls
        sliderROrH = baseControl.GetChild(0).GetChild<HSlider>(1);
        sliderGOrS = baseControl.GetChild(1).GetChild<HSlider>(1);
        sliderBOrV = baseControl.GetChild(2).GetChild<HSlider>(1);
        sliderA = baseControl.GetChild(3).GetChild<HSlider>(1);
        pickerButton = GetChild(1).GetChild<ToolButton>(1);
        hsvCheckBox = GetNode<CustomCheckBox>("MarginButtonsContainer/ButtonsContainer/HSVCheckBox");
        rawCheckBox = GetNode<CustomCheckBox>("MarginButtonsContainer/ButtonsContainer/RawCheckBox");
        htmlColourStart = GetNode<Label>("MarginButtonsContainer/ButtonsContainer/HtmlColourStart");
        htmlColourEdit = GetNode<LineEdit>("MarginButtonsContainer/ButtonsContainer/HtmlColourEdit");
        separator = GetNode<HSeparator>("Separator");
        presetsContainer = GetNode<GridContainer>("PresetContainer");
        addPresetButton = GetNode<TextureButton>("PresetButtonContainer/AddPresetButton");

        // Picker button logic rewrite #3055
        // TODO: Revert this PR once https://github.com/godotengine/godot/issues/57343 is solved.
        baseControl = GetChild(1);
        var customPickerButton = new ToolButton { Icon = pickerButton.Icon };
        pickerButton.Hide();
        baseControl.AddChild(customPickerButton);
        pickerButton = customPickerButton;
        pickerButton.Connect("pressed", this, nameof(OnPickerClicked));

        // Update control state.
        UpdateButtonsState();
        PresetsEditable = presetsEditable;
        PresetsVisible = presetsVisible;
        OnColourChanged(Color);

        // Disable value bar scroll with the mouse, as the colour pickers are often in scrollable containers and
        // this would otherwise be problematic. Perhaps in the future we should have this be configurable with an
        // export property?
        sliderROrH.Scrollable = false;
        sliderGOrS.Scrollable = false;
        sliderBOrV.Scrollable = false;
        sliderA.Scrollable = false;

        // Load presets.
        if (PresetsStorage.TryGetValue(PresetGroup, out groupStorage))
        {
            foreach (var colour in groupStorage)
                OnGroupAddPreset(colour);
        }
        else
        {
            // Always ensure there is one so then we just modify it instead of having to check.
            groupStorage = new PresetGroupStorage(GetPresets());
            PresetsStorage.Add(PresetGroup, groupStorage);
        }

        // Add current preset handlers to preset group
        groupStorage.AddPresetDelegate += OnGroupAddPreset;
        groupStorage.ErasePresetDelegate += OnGroupErasePreset;

        UpdateTooltips();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (pickingColor)
        {
            HandleActiveColourPicking(delta);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (pickingColor)
        {
            if (@event is InputEventMouse { ButtonMask: (int)ButtonList.Left })
            {
                // Confirm, perform the last pick so that the colour is precisely the pixel clicked
                HandleActiveColourPicking(Constants.COLOUR_PICKER_PICK_INTERVAL);
                pickingColor = false;
            }
            else if (@event is InputEventKey { Scancode: (int)KeyList.Escape, Pressed: true }
                     or InputEventMouse { ButtonMask: (int)ButtonList.Right })
            {
                // Cancel
                pickingColor = false;
                Color = colorBeforePicking;
            }

            AcceptEvent();
        }

        base._Input(@event);
    }

    public override void _ExitTree()
    {
        groupStorage.AddPresetDelegate -= OnGroupAddPreset;
        groupStorage.ErasePresetDelegate -= OnGroupErasePreset;
        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateTooltips();

        base._Notification(what);
    }

    /// <summary>
    ///   Adds a preset to the group this picker is in.
    ///   If this preset already exists, no act will be taken.
    /// </summary>
    /// <param name="colour">Colour of the preset to be added</param>
    public new void AddPreset(Color colour)
    {
        // Broadcast to all group numbers.
        groupStorage.AddPreset(colour);
    }

    /// <summary>
    ///   Deletes a preset from the group this picker is in.
    ///   If no such preset exists, no act will be taken.
    /// </summary>
    /// <param name="colour">Colour of the preset to be removed</param>
    public new void ErasePreset(Color colour)
    {
        groupStorage.ErasePreset(colour);
    }

    private void OnGroupAddPreset(Color colour)
    {
        if (presetsContainer == null)
            throw new InvalidOperationException("This colour picker is not initialized yet");

        // Add preset locally
        var preset = new TweakedColourPickerPreset(this, colour);
        presets.Add(preset);
        presetsContainer.AddChild(preset);

        // Add preset to base class
        base.AddPreset(colour);
    }

    private void OnGroupErasePreset(Color colour)
    {
        if (presetsContainer == null)
            throw new InvalidOperationException("This colour picker is not initialized yet");

        var preset = presets.First(p => p.Color == colour);
        presets.Remove(preset);
        presetsContainer.RemoveChild(preset);
        preset.QueueFree();

        base.ErasePreset(colour);
    }

    /// <summary>
    ///   When return from raw mode make sure the three values are within RGB standard. (Maximum value 1)
    /// </summary>
    private void ValidateRgbColor()
    {
        var colour = Color;

        if (colour.r > 1)
            colour.r = 1;
        if (colour.g > 1)
            colour.g = 1;
        if (colour.b > 1)
            colour.b = 1;

        Color = colour;
    }

    private void UpdateTooltips()
    {
        if (addPresetButton == null || hsvCheckBox == null)
            return;

        pickerButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_PICK_COLOUR");
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_ADD_PRESET");
        hsvCheckBox.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_HSV_BUTTON_TOOLTIP");
        rawCheckBox.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_RAW_BUTTON_TOOLTIP");

        if (HsvMode)
        {
            sliderROrH.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_H_TOOLTIP");
            sliderGOrS.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_S_TOOLTIP");
            sliderBOrV.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_V_TOOLTIP");
        }
        else
        {
            sliderROrH.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_R_TOOLTIP");
            sliderGOrS.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_G_TOOLTIP");
            sliderBOrV.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_B_TOOLTIP");
        }

        sliderA.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_A_TOOLTIP");
    }

    private void UpdateButtonsState()
    {
        if (hsvCheckBox == null)
            return;

        switch (mode)
        {
            case PickerMode.Rgb:
            {
                hsvCheckBox.Pressed = false;
                rawCheckBox.Pressed = false;
                break;
            }

            case PickerMode.Hsv:
            {
                rawCheckBox.Pressed = false;
                hsvCheckBox.Pressed = true;
                break;
            }

            case PickerMode.Raw:
            {
                hsvCheckBox.Pressed = false;
                rawCheckBox.Pressed = true;
                break;
            }

            default:
            {
                throw new NotImplementedException();
            }
        }

        hsvCheckBox.Disabled = !hsvButtonEnabled;
        rawCheckBox.Disabled = !rawButtonEnabled;
    }

    private void OnAddPresetButtonPressed()
    {
        if (!presetsEditable)
            return;

        AddPreset(Color);
    }

    private void OnHSVButtonToggled(bool isOn)
    {
        HsvMode = isOn;
    }

    private void OnRawButtonToggled(bool isOn)
    {
        RawMode = isOn;
    }

    private void OnColourChanged(Color colour)
    {
        // Hide HtmlColourEdit color change when Raw mode is on and any color value is above 1.0
        htmlColourStart.Visible = htmlColourEdit.Visible = !(mode == PickerMode.Raw && colour.IsRaw());

        htmlColourEdit.Text = colour.ToHtml();
    }

    /// <summary>
    ///   Called when (keyboard) entered in HtmlColourEdit or from OnHtmlColourEditFocusExited.
    ///   Set Color when text is valid; reset if not.
    /// </summary>
    /// <param name="colour">Current htmlColourEditor text</param>
    private void OnHtmlColourEditEntered(string colour)
    {
        if (colour.IsValidHtmlColor())
        {
            Color = new Color(colour);
        }
        else
        {
            htmlColourEdit.Text = Color.ToHtml();
        }
    }

    /// <summary>
    ///   Called when focus exited HtmlColourEdit.
    /// </summary>
    private void OnHtmlColourEditFocusExited()
    {
        OnHtmlColourEditEntered(htmlColourEdit.Text);
        htmlColourEdit.Deselect();
    }

    private void OnPickerClicked()
    {
        colorBeforePicking = Color;
        pickingColor = true;
    }

    private void HandleActiveColourPicking(float delta)
    {
        pickerTimeElapsed += delta;

        if (pickerTimeElapsed < Constants.COLOUR_PICKER_PICK_INTERVAL)
            return;

        var viewportTexture = GetViewport().GetTexture().GetData();
        var viewportRect = GetViewportRect();
        var scale = viewportRect.End.x / viewportTexture.GetSize().x;
        var position = GetGlobalMousePosition() / scale;
        position.y = viewportTexture.GetHeight() - position.y;

        viewportTexture.Lock();
        Color = viewportTexture.GetPixelv(position);
        viewportTexture.Unlock();

        pickerTimeElapsed = 0;
    }

    private class TweakedColourPickerPreset : ColorRect
    {
        private readonly TweakedColourPicker owner;

        public TweakedColourPickerPreset(TweakedColourPicker owner, Color colour)
        {
            this.owner = owner;
            Color = colour;

            // Init the GUI part of the ColorRect
            MarginTop = MarginBottom = MarginLeft = MarginRight = 6.0f;
            RectMinSize = new Vector2(20, 20);
            SizeFlagsHorizontal = (int)SizeFlags.ShrinkEnd;
            SizeFlagsVertical = (int)SizeFlags.ShrinkCenter;
            UpdateTooltip();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationTranslationChanged)
                UpdateTooltip();

            base._Notification(what);
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton { Pressed: true } mouseEvent)
            {
                switch ((ButtonList)mouseEvent.ButtonIndex)
                {
                    case ButtonList.Left:
                    {
                        GetTree().SetInputAsHandled();
                        owner.Color = Color;
                        return;
                    }

                    case ButtonList.Right:
                    {
                        if (!owner.PresetsEditable)
                            break;

                        GetTree().SetInputAsHandled();
                        owner.ErasePreset(Color);
                        return;
                    }
                }
            }

            base._GuiInput(inputEvent);
        }

        private void UpdateTooltip()
        {
            HintTooltip = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("COLOUR_PICKER_PRESET_TOOLTIP"),
                Color.IsRaw() ? "argb(" + Color + ")" : "#" + Color.ToHtml());
        }
    }

    private class PresetGroupStorage : IEnumerable<Color>
    {
        private readonly List<Color> colours;

        public PresetGroupStorage(IEnumerable<Color> colours)
        {
            this.colours = colours.ToList();
        }

        public AddPresetDelegate? AddPresetDelegate { get; set; }

        public DeletePresetDelegate? ErasePresetDelegate { get; set; }

        public void AddPreset(Color colour)
        {
            if (colours.Contains(colour))
                return;

            colours.Add(colour);

            AddPresetDelegate?.Invoke(colour);
        }

        public void ErasePreset(Color colour)
        {
            colours.Remove(colour);
            ErasePresetDelegate?.Invoke(colour);
        }

        public IEnumerator<Color> GetEnumerator()
        {
            return colours.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
