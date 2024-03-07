using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Tweaked colour picker allows for tooltip texts translations and has a customized theming.
/// </summary>
/// <remarks>
///   <para>
///     TODO: add support for the new OKHSL mode in Godot
///   </para>
/// </remarks>
public partial class TweakedColourPicker : ColorPicker
{
    /// <summary>
    ///   This is where presets are stored after a colour picker exited scene tree.
    ///   <remarks>
    ///     <para>
    ///       The Key is the group name; <br />
    ///       The Value is the preset storage.
    ///     </para>
    ///   </remarks>
    /// </summary>
    private static readonly Dictionary<string, PresetGroupStorage> PresetsStorage = new();

    /// <summary>
    ///   This is the local storage of its preset children
    ///   so that we don't need to call GetChildren() when deleting one.
    /// </summary>
    private readonly List<TweakedColourPickerPreset> presets = new();

#pragma warning disable CA2213
    private HSlider sliderROrH = null!;
    private HSlider sliderGOrS = null!;
    private HSlider sliderBOrV = null!;
    private HSlider sliderA = null!;
    private Button pickerButton = null!;
    private CustomCheckBox? hsvCheckBox;
    private CustomCheckBox rawCheckBox = null!;
    private Label htmlColourStart = null!;
    private LineEdit htmlColourEdit = null!;
    private HSeparator separator = null!;
    private GridContainer? presetsContainer;
    private TextureButton? addPresetButton;
#pragma warning restore CA2213

    private bool hsvButtonEnabled = true;
    private bool rawButtonEnabled = true;
    private bool presetsEditable = true;
    private bool presetsVisible = true;

    // TODO:
    private ColorModeType mode = ColorModeType.Rgb;

    private bool pickingColor;
    private Color colorBeforePicking;
    private double pickerTimeElapsed;

    private PresetGroupStorage? groupStorage;

    private delegate void AddPresetDelegate(Color colour);

    private delegate void DeletePresetDelegate(Color colour);

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
    ///   Change the picker's colour mode.
    /// </summary>
    [Export]
    public new ColorModeType ColorMode
    {
        get => base.ColorMode;
        set
        {
            mode = value;
            if (value != ColorModeType.Raw)
                ValidateRgbColor();

            base.ColorMode = mode;
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

    // TODO: fix this with Godot 4
    /// <summary>
    ///   Hide Godot property PresetsEnabled to avoid unexpected changes
    ///   which may cause the hidden native buttons reappear.
    /// </summary>
    private new bool PresetsEnabled { get => PresetsEditable; set => PresetsEditable = value; }

    public override void _Ready()
    {
        base._Ready();

        // Hide replaced native controls. Can't delete them because it will crash Godot.
        // TODO: fix this: this no longer has the same child layout
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
        pickerButton = GetChild(1).GetChild<Button>(1);
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
        var customPickerButton = new Button { Icon = pickerButton.Icon };
        pickerButton.Hide();
        baseControl.AddChild(customPickerButton);
        pickerButton = customPickerButton;
        pickerButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnPickerClicked)));

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

    public override void _ExitTree()
    {
        if (groupStorage != null)
        {
            groupStorage.AddPresetDelegate -= OnGroupAddPreset;
            groupStorage.ErasePresetDelegate -= OnGroupErasePreset;
        }

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (pickingColor)
        {
            HandleActiveColourPicking(delta);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateTooltips();

        base._Notification(what);
    }

    public override void _Input(InputEvent @event)
    {
        if (pickingColor)
        {
            if (@event is InputEventMouse { ButtonMask: MouseButtonMask.Left })
            {
                // Confirm, perform the last pick so that the colour is precisely the pixel clicked
                HandleActiveColourPicking(Constants.COLOUR_PICKER_PICK_INTERVAL);
                pickingColor = false;
            }
            else if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true }
                     or InputEventMouse { ButtonMask: MouseButtonMask.Right })
            {
                // Cancel
                pickingColor = false;
                SetColour(colorBeforePicking);
            }

            AcceptEvent();
        }

        base._Input(@event);
    }

    /// <summary>
    ///   Change the TweakedColourPicker's current colour.
    ///   <remarks>
    ///     <para>
    ///       This emits the color_changed signal, used by the custom colour picker preset when it's selected so
    ///       that other elements can be notified of the newly selected colour.
    ///     </para>
    ///   </remarks>
    /// </summary>
    public void SetColour(Color colour)
    {
        if (Color == colour)
            return;

        Color = colour;
        EmitSignal("color_changed", colour);
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

        if (colour.R > 1)
            colour.R = 1;
        if (colour.G > 1)
            colour.G = 1;
        if (colour.B > 1)
            colour.B = 1;

        SetColour(colour);
    }

    private void UpdateTooltips()
    {
        if (addPresetButton == null || hsvCheckBox == null)
            return;

        pickerButton.TooltipText = Localization.Translate("COLOUR_PICKER_PICK_COLOUR");
        addPresetButton.TooltipText = Localization.Translate("COLOUR_PICKER_ADD_PRESET");
        hsvCheckBox.TooltipText = Localization.Translate("COLOUR_PICKER_HSV_BUTTON_TOOLTIP");
        rawCheckBox.TooltipText = Localization.Translate("COLOUR_PICKER_RAW_BUTTON_TOOLTIP");

        if (mode == ColorModeType.Hsv)
        {
            sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_H_TOOLTIP");
            sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_S_TOOLTIP");
            sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_V_TOOLTIP");
        }
        else
        {
            sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_R_TOOLTIP");
            sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_G_TOOLTIP");
            sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_B_TOOLTIP");
        }

        sliderA.TooltipText = Localization.Translate("COLOUR_PICKER_A_TOOLTIP");
    }

    private void UpdateButtonsState()
    {
        if (hsvCheckBox == null)
            return;

        switch (mode)
        {
            case ColorModeType.Rgb:
            {
                hsvCheckBox.ButtonPressed = false;
                rawCheckBox.ButtonPressed = false;
                break;
            }

            case ColorModeType.Hsv:
            {
                rawCheckBox.ButtonPressed = false;
                hsvCheckBox.ButtonPressed = true;
                break;
            }

            case ColorModeType.Raw:
            {
                hsvCheckBox.ButtonPressed = false;
                rawCheckBox.ButtonPressed = true;
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
        if (!isOn)
        {
            ColorMode = ColorModeType.Rgb;
        }
        else
        {
            ColorMode = ColorModeType.Hsv;
        }
    }

    private void OnRawButtonToggled(bool isOn)
    {
        if (!isOn)
        {
            ColorMode = ColorModeType.Rgb;
        }
        else
        {
            ColorMode = ColorModeType.Raw;
        }
    }

    private void OnColourChanged(Color colour)
    {
        // Hide HtmlColourEdit color change when Raw mode is on and any color value is above 1.0
        htmlColourStart.Visible = htmlColourEdit.Visible = !(mode == ColorModeType.Raw && colour.IsRaw());

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
            SetColour(new Color(colour));
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

    private void HandleActiveColourPicking(double delta)
    {
        pickerTimeElapsed += delta;

        if (pickerTimeElapsed < Constants.COLOUR_PICKER_PICK_INTERVAL)
            return;

        var viewportTexture = GetViewport().GetTexture().GetImage();
        var viewportRect = GetViewportRect();
        var scale = viewportRect.End.X / viewportTexture.GetSize().X;
        var position = GetGlobalMousePosition() / scale;
        position.Y = viewportTexture.GetHeight() - position.Y;

        SetColour(viewportTexture.GetPixelv(position.RoundedInt()));

        pickerTimeElapsed = 0;
    }

    private partial class TweakedColourPickerPreset : ColorRect
    {
        private readonly TweakedColourPicker owner;

        public TweakedColourPickerPreset(TweakedColourPicker owner, Color colour)
        {
            this.owner = owner;
            Color = colour;

            // Init the GUI part of the ColorRect
            OffsetTop = OffsetBottom = OffsetLeft = OffsetRight = 6.0f;
            CustomMinimumSize = new Vector2(20, 20);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
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
                switch (mouseEvent.ButtonIndex)
                {
                    case MouseButton.Left:
                    {
                        GetViewport().SetInputAsHandled();
                        owner.SetColour(Color);
                        return;
                    }

                    case MouseButton.Right:
                    {
                        if (!owner.PresetsEditable)
                            break;

                        GetViewport().SetInputAsHandled();
                        owner.ErasePreset(Color);
                        return;
                    }
                }
            }

            base._GuiInput(inputEvent);
        }

        private void UpdateTooltip()
        {
            TooltipText = Localization.Translate("COLOUR_PICKER_PRESET_TOOLTIP")
                .FormatSafe(Color.IsRaw() ? "argb(" + Color + ")" : "#" + Color.ToHtml());
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
