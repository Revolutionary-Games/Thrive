using System;
using System.Globalization;
using Godot;
using Saving;

/// <summary>
///   An item in the save list. This is a class to handle loading its data from the file
/// </summary>
public partial class SaveListItem : PanelContainer
{
    public static readonly object ResizeLock = new();

    [Export]
    public bool Selectable;

    [Export]
    public bool Loadable = true;

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
    private bool dataLoaded;
    private SaveInfoAndScreenshot? saveInfoLoadTask;

    private bool highlighted;
    private bool selected;

    private bool isBroken;
    private bool isKnownIncompatible;
    private bool isUpgradeable;
    private bool isIncompatiblePrototype;
    private bool isKnownCompatible;

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

    public override void _ExitTree()
    {
        base._ExitTree();

        CancelLoad();
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

    public void TriggerLoad()
    {
        if (dataLoaded || loadingData || saveInfoLoadTask != null)
            return;

        LoadSaveData();
    }

    public void CancelLoad()
    {
        if (dataLoaded || saveInfoLoadTask == null)
            return;

        var loadTask = saveInfoLoadTask;
        loadTask.OnComplete = null;

        if (!loadTask.Loaded)
            ResourceManager.Instance.CancelLoad(loadTask);

        saveInfoLoadTask = null;
        loadingData = false;
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
            if (isKnownCompatible)
            {
                EmitSignal(SignalName.OnProblemFreeSaveLoaded);
            }
            else
            {
                EmitSignal(SignalName.OnOldSaveLoaded);
            }

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

        var loadTask = new SaveInfoAndScreenshot(saveName);
        loadTask.OnComplete = OnSaveInfoLoaded;
        saveInfoLoadTask = loadTask;

        // Resource manager is now used to limit how big of a lag spike opening the pause menu causes
        ResourceManager.Instance.QueueLoad(loadTask);
    }

    private void OnSaveInfoLoaded(IResource resource)
    {
        if (resource is not SaveInfoAndScreenshot loadedResource ||
            !ReferenceEquals(saveInfoLoadTask, loadedResource))
        {
            return;
        }

        // Release item ownership before touching the UI so a callback failure cannot leave this item stuck or replay.
        loadedResource.OnComplete = null;
        saveInfoLoadTask = null;
        loadingData = false;

        var save = loadedResource.Save ?? throw new Exception("Save info resource didn't load a save instance");

        isBroken = save.Info.Type == SaveInformation.SaveType.Invalid;

        // Screenshot (if present, saves can have a missing screenshot)
        if (loadedResource.Screenshot != null)
            screenshot.Texture = loadedResource.Screenshot;

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
            versionWarning.Visible = true;

            // Check if the version is known compatible
            if (CompatibleSaveVersions.IsMarkedCompatible(save.Info.ThriveVersion, save.Info.IsPrototype))
            {
                versionWarning.Visible = false;
                isKnownCompatible = true;
            }
            else
            {
                // Not explicitly marked compatible, but might be loadable

                if (save.Info.IsPrototype)
                {
                    // Disallowed save to try to load from a different version due to being a prototype
                    isIncompatiblePrototype = true;
                }
                else if (versionDifference < 0 && SaveUpgrader.CanUpgradeSaveToVersion(save.Info))
                {
                    isUpgradeable = true;
                }
            }

            if (SaveHelper.IsKnownIncompatible(save.Info.ThriveVersion))
            {
                isKnownIncompatible = true;
                versionWarning.Visible = true;
                isKnownCompatible = false;
            }
        }
        else
        {
            versionWarning.Visible = false;
        }

        version.Text = save.Info.ThriveVersion;

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

        dataLoaded = true;
    }

    private void UpdateName()
    {
        saveNameLabel?.Text = saveName.Replace(Constants.SAVE_EXTENSION_WITH_DOT, string.Empty);
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
        highlightPanel?.Visible = Highlighted || Selected;
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

        public bool RequiresSyncLoad => false;
        public bool UsesPostProcessing => true;

        public bool RequiresSyncPostProcess => true;

        public bool CancelRequested { get; set; }

        public float EstimatedTimeRequired => 0.025f;
        public bool LoadingPrepared { get; set; }
        public bool Loaded { get; private set; }
        public string Identifier => $"{nameof(SaveInfoAndScreenshot)}/{saveName}";

        public Action<IResource>? OnComplete { get; set; }

        public Save? Save { get; private set; }
        public ImageTexture? Screenshot { get; private set; }

        public void PrepareLoading()
        {
            data = Save.LoadInfoAndRawScreenshotFromSave(saveName);
        }

        public void Load()
        {
            var loadData = data!.Value;

            try
            {
                Save = Save.ConstructSaveFromInfoAndScreenshotBuffer(saveName, loadData.Info,
                    loadData.ScreenshotData);
            }
            catch (Exception e)
            {
                // A broken thumbnail should not prevent the save's metadata or later saves from loading.
                GD.PrintErr($"Failed to decode screenshot for save {saveName}: ", e);
                Save = Save.ConstructSaveFromInfoAndScreenshotBuffer(saveName, loadData.Info, null);
            }
            finally
            {
                // Let go of the raw archive data before waiting for the main-thread phase.
                data = null;
            }
        }

        public void PerformPostProcessing()
        {
            try
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
                        lock (ResizeLock)
                        {
                            Save.Screenshot.Resize((int)(Constants.SAVE_LIST_SCREENSHOT_HEIGHT * aspectRatio),
                                Constants.SAVE_LIST_SCREENSHOT_HEIGHT);
                        }
                    }

                    Screenshot = ImageTexture.CreateFromImage(Save.Screenshot);
                }
            }
            catch (Exception e)
            {
                // Preserve save metadata when resize or texture creation fails; the list can show no thumbnail.
                Screenshot = null;
                GD.PrintErr($"Failed to prepare screenshot for save {saveName}: ", e);
            }
            finally
            {
                Loaded = true;
            }
        }

        public void UnLoad()
        {
            data = null;
            Loaded = false;
            Save = null;
            Screenshot = null;
            OnComplete = null;
        }
    }
}
