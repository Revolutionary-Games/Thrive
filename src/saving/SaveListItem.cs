using System;
using System.Globalization;
using Godot;

/// <summary>
///   An item in the saves list. This is a class to handle loading its data from the file
/// </summary>
public partial class SaveListItem : PanelContainer
{
    [Export]
    public bool Selectable;

    [Export]
    public bool Loadable = true;

    private static readonly object ResizeLock = new();

#pragma warning disable CA2213
    [Export]
    private Label? saveNameLabel;

    [Export]
    private TextureRect screenshot = null!;

    [Export]
    private Label version = null!;

    [Export]
    private Label versionWarning = null!;

    [Export]
    private Label type = null!;

    [Export]
    private Label createdAt = null!;

    [Export]
    private Label createdBy = null!;

    [Export]
    private Label createdOnPlatform = null!;

    [Export]
    private Label tags = null!;

    [Export]
    private Label description = null!;

    [Export]
    private Button loadButton = null!;

    [Export]
    private Panel? highlightPanel;
#pragma warning restore CA2213

    private string saveName = string.Empty;
    private int versionDifference;

    private bool loadingData;
    private SaveInfoAndScreenshot? saveInfoLoadTask;

    private bool highlighted;
    private bool selected;

    private bool isBroken;
    private bool isKnownIncompatible;
    private bool isUpgradeable;
    private bool isIncompatiblePrototype;

    [Signal]
    public delegate void OnSelectedChangedEventHandler();

    [Signal]
    public delegate void OnDoubleClickedEventHandler();

    [Signal]
    public delegate void OnDeletedEventHandler();

    [Signal]
    public delegate void OnOldSaveLoadedEventHandler();

    [Signal]
    public delegate void OnUpgradeableSaveLoadedEventHandler(string saveName, bool incompatible);

    [Signal]
    public delegate void OnBrokenSaveLoadedEventHandler();

    [Signal]
    public delegate void OnNewSaveLoadedEventHandler();

    [Signal]
    public delegate void OnKnownIncompatibleLoadedEventHandler();

    [Signal]
    public delegate void OnDifferentVersionPrototypeLoadedEventHandler();

    /// <summary>
    ///   Triggered when this is loaded without a problem. This is triggered when the load is already in progress,
    ///   so this is more of an informative callback for components that need to know when a save load was done.
    /// </summary>
    [Signal]
    public delegate void OnProblemFreeSaveLoadedEventHandler(string saveName);

    public string SaveName
    {
        get => saveName;
        set
        {
            if (value == saveName)
                return;

            saveName = value;
            LoadSaveData();
            UpdateName();
        }
    }

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;
            UpdateHighlighting();
        }
    }

    public bool Selected
    {
        get
        {
            if (!Selectable)
                return false;

            return selected;
        }
        set
        {
            if (!Selectable)
                throw new InvalidOperationException();

            selected = value;
            UpdateHighlighting();
        }
    }

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(SaveName))
            throw new InvalidOperationException($"{nameof(SaveName)} is required");

        loadButton.Visible = Loadable;

        UpdateName();
        UpdateHighlighting();
    }

    public override void _Process(double delta)
    {
        if (!loadingData)
            return;

        if (!saveInfoLoadTask!.Loaded)
            return;

        var save = saveInfoLoadTask.Save ?? throw new Exception("Save info resource didn't load a save instance");
        var screenshotImage = saveInfoLoadTask.Screenshot;
        saveInfoLoadTask = null;

        isBroken = save.Info.Type == SaveInformation.SaveType.Invalid;

        // Screenshot (if present, saves can have a missing screenshot)
        if (screenshotImage != null)
        {
            screenshot.Texture = screenshotImage;
        }

        // General info

        // If save is valid, compare version numbers
        if (!isBroken)
        {
            versionDifference = VersionUtils.Compare(save.Info.ThriveVersion, Constants.Version);
        }
        else
        {
            versionDifference = 0;
        }

        if (versionDifference != 0)
        {
            if (save.Info.IsPrototype)
            {
                isIncompatiblePrototype = true;
            }
            else
            {
                if (versionDifference < 0 && SaveUpgrader.CanUpgradeSaveToVersion(save.Info))
                {
                    isUpgradeable = true;
                }

                if (SaveHelper.IsKnownIncompatible(save.Info.ThriveVersion))
                {
                    isKnownIncompatible = true;
                }
            }
        }

        version.Text = save.Info.ThriveVersion;
        versionWarning.Visible = versionDifference != 0;
        type.Text = save.Info.TranslatedSaveTypeString;
        createdAt.Text = save.Info.CreatedAt.ToString("G", CultureInfo.CurrentCulture);
        createdBy.Text = save.Info.Creator;
        createdOnPlatform.Text = save.Info.Platform;
        description.Text = save.Info.Description;

        if (save.Info.CheatsUsed)
        {
            tags.Visible = true;
            tags.Text = Localization.Translate("SAVE_CHEATS_USED");
        }
        else
        {
            tags.Visible = false;
        }

        loadingData = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
        {
            AcceptEvent();

            if (mouse.DoubleClick)
            {
                EmitSignal(SignalName.OnDoubleClicked);
            }
            else
            {
                OnSelect();
            }
        }
    }

    public void LoadThisSave()
    {
        if (isBroken)
        {
            EmitSignal(SignalName.OnBrokenSaveLoaded);
            return;
        }

        if (isIncompatiblePrototype)
        {
            EmitSignal(SignalName.OnDifferentVersionPrototypeLoaded);
            return;
        }

        if (versionDifference < 0 && isUpgradeable)
        {
            EmitSignal(SignalName.OnUpgradeableSaveLoaded, SaveName, isKnownIncompatible);
            return;
        }

        if (isKnownIncompatible)
        {
            EmitSignal(SignalName.OnKnownIncompatibleLoaded);
            return;
        }

        if (versionDifference < 0)
        {
            EmitSignal(SignalName.OnOldSaveLoaded);
            return;
        }

        if (versionDifference > 0)
        {
            EmitSignal(SignalName.OnNewSaveLoaded);
            return;
        }

        EmitSignal(SignalName.OnProblemFreeSaveLoaded);
    }

    private void LoadSaveData()
    {
        loadingData = true;

        saveInfoLoadTask = new SaveInfoAndScreenshot(saveName);

        // Resource manager is now used to limit how big of a lag spike opening the pause menu causes
        ResourceManager.Instance.QueueLoad(saveInfoLoadTask);
    }

    private void UpdateName()
    {
        if (saveNameLabel != null)
            saveNameLabel.Text = saveName.Replace(Constants.SAVE_EXTENSION_WITH_DOT, string.Empty);
    }

    private void LoadSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        LoadThisSave();
    }

    private void OnSelect()
    {
        if (!Selectable)
            return;

        Selected = !Selected;

        EmitSignal(SignalName.OnSelectedChanged);
    }

    private void OnMouseEnter()
    {
        Highlighted = true;
    }

    private void OnMouseExit()
    {
        Highlighted = false;
    }

    private void UpdateHighlighting()
    {
        if (highlightPanel == null)
            return;

        highlightPanel.Visible = Highlighted || Selected;
    }

    private void DeletePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnDeleted);
    }

    private class SaveInfoAndScreenshot : IResource
    {
        private readonly string saveName;
        private (SaveInformation Info, byte[]? ScreenshotData)? data;

        public SaveInfoAndScreenshot(string saveName)
        {
            this.saveName = saveName;
        }

        // See the TODO comment in PerformPostProcessing
        public bool RequiresSyncLoad => true;
        public bool UsesPostProcessing => true;

        // See the TODO comment in PerformPostProcessing
        public bool RequiresSyncPostProcess => true;
        public float EstimatedTimeRequired => 0.025f;
        public bool LoadingPrepared { get; set; }
        public bool Loaded { get; private set; }
        public string Identifier => $"{nameof(SaveInfoAndScreenshot)}/{saveName}";

        // TODO: maybe this should switch to using the callback to update the state rather than _Process?
        public Action<IResource>? OnComplete { get; set; }

        public Save? Save { get; private set; }
        public ImageTexture? Screenshot { get; private set; }

        public void PrepareLoading()
        {
            data = Save.LoadInfoAndRawScreenshotFromSave(saveName);
        }

        public void Load()
        {
            Save = Save.ConstructSaveFromInfoAndScreenshotBuffer(saveName, data!.Value.Info, data.Value.ScreenshotData);

            // Let go of the data
            data = null;
        }

        public void PerformPostProcessing()
        {
            if (Save!.Screenshot != null)
            {
                // Rescale the screenshot to save memory etc.
                float aspectRatio = Save.Screenshot.GetWidth() / (float)Save.Screenshot.GetHeight();

                if (Save.Screenshot.GetHeight() > Constants.SAVE_LIST_SCREENSHOT_HEIGHT)
                {
                    // TODO: this seems like a Godot bug, the game crashes often when loading the saves list without
                    // this lock. See: https://github.com/godotengine/godot/issues/55528
                    // Partly resolves: https://github.com/Revolutionary-Games/Thrive/issues/2078
                    // but not for all people and save amounts
                    // Note that now as an additional workaround this uses the resource manager with sync loading
                    lock (ResizeLock)
                    {
                        Save.Screenshot.Resize((int)(Constants.SAVE_LIST_SCREENSHOT_HEIGHT * aspectRatio),
                            Constants.SAVE_LIST_SCREENSHOT_HEIGHT);
                    }
                }

                Screenshot = ImageTexture.CreateFromImage(Save.Screenshot);
            }

            Loaded = true;
        }

        public void UnLoad()
        {
            Loaded = false;
            Save = null;
            Screenshot = null;
        }
    }
}
