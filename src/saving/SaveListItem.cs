using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   An item in the saves list. This is a class to handle loading its data from the file
/// </summary>
public class SaveListItem : PanelContainer
{
    [Export]
    public bool Selectable;

    [Export]
    public bool Loadable = true;

    [Export]
    public NodePath SaveNamePath = null!;

    [Export]
    public NodePath ScreenshotPath = null!;

    [Export]
    public NodePath VersionPath = null!;

    [Export]
    public NodePath VersionWarningPath = null!;

    [Export]
    public NodePath TypePath = null!;

    [Export]
    public NodePath CreatedAtPath = null!;

    [Export]
    public NodePath CreatedByPath = null!;

    [Export]
    public NodePath CreatedOnPlatformPath = null!;

    [Export]
    public NodePath DescriptionPath = null!;

    [Export]
    public NodePath LoadButtonPath = null!;

    [Export]
    public NodePath HighlightPath = null!;

    private static readonly object ResizeLock = new();

    private Label? saveNameLabel;
    private TextureRect screenshot = null!;
    private Label version = null!;
    private Label versionWarning = null!;
    private Label type = null!;
    private Label createdAt = null!;
    private Label createdBy = null!;
    private Label createdOnPlatform = null!;
    private Label description = null!;
    private Button loadButton = null!;
    private Panel? highlightPanel;

    private string saveName = string.Empty;
    private int versionDifference;

    private bool loadingData;
    private Task<Save>? saveInfoLoadTask;

    private bool highlighted;
    private bool selected;

    private bool isBroken;
    private bool isKnownIncompatible;
    private bool isUpgradeable;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnDoubleClicked();

    [Signal]
    public delegate void OnDeleted();

    [Signal]
    public delegate void OnOldSaveLoaded();

    [Signal]
    public delegate void OnUpgradeableSaveLoaded(string saveName, bool incompatible);

    [Signal]
    public delegate void OnBrokenSaveLoaded();

    [Signal]
    public delegate void OnNewSaveLoaded();

    [Signal]
    public delegate void OnKnownIncompatibleLoaded();

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

        saveNameLabel = GetNode<Label>(SaveNamePath);
        screenshot = GetNode<TextureRect>(ScreenshotPath);
        version = GetNode<Label>(VersionPath);
        versionWarning = GetNode<Label>(VersionWarningPath);
        type = GetNode<Label>(TypePath);
        createdAt = GetNode<Label>(CreatedAtPath);
        createdBy = GetNode<Label>(CreatedByPath);
        createdOnPlatform = GetNode<Label>(CreatedOnPlatformPath);
        description = GetNode<Label>(DescriptionPath);
        loadButton = GetNode<Button>(LoadButtonPath);
        highlightPanel = GetNode<Panel>(HighlightPath);

        loadButton.Visible = Loadable;

        UpdateName();
        UpdateHighlighting();
    }

    public override void _Process(float delta)
    {
        if (!loadingData)
            return;

        if (!saveInfoLoadTask!.IsCompleted)
            return;

        var save = saveInfoLoadTask.Result;
        saveInfoLoadTask.Dispose();
        saveInfoLoadTask = null;

        isBroken = save.Info.Type == SaveInformation.SaveType.Invalid;

        // Screenshot (if present, saves can have a missing screenshot)
        if (save.Screenshot != null)
        {
            var texture = new ImageTexture();
            texture.CreateFromImage(save.Screenshot);

            screenshot.Texture = texture;
        }

        // General info

        // If save is valid compare version numbers
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
            if (versionDifference < 0 && SaveUpgrader.CanUpgradeSaveToVersion(save.Info))
            {
                isUpgradeable = true;
            }

            if (SaveHelper.IsKnownIncompatible(save.Info.ThriveVersion))
            {
                isKnownIncompatible = true;
            }
        }

        version.Text = save.Info.ThriveVersion;
        versionWarning.Visible = versionDifference != 0;
        type.Text = save.Info.TranslatedSaveTypeString;
        createdAt.Text = save.Info.CreatedAt.ToString("G", CultureInfo.CurrentCulture);
        createdBy.Text = save.Info.Creator;
        createdOnPlatform.Text = save.Info.Platform;
        description.Text = save.Info.Description;

        loadingData = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: (int)ButtonList.Left } mouse)
        {
            AcceptEvent();

            if (mouse.Doubleclick)
            {
                EmitSignal(nameof(OnDoubleClicked));
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
            EmitSignal(nameof(OnBrokenSaveLoaded));
            return;
        }

        if (versionDifference < 0 && isUpgradeable)
        {
            EmitSignal(nameof(OnUpgradeableSaveLoaded), SaveName, isKnownIncompatible);
            return;
        }

        if (isKnownIncompatible)
        {
            EmitSignal(nameof(OnKnownIncompatibleLoaded));
            return;
        }

        if (versionDifference < 0)
        {
            EmitSignal(nameof(OnOldSaveLoaded));
            return;
        }

        if (versionDifference > 0)
        {
            EmitSignal(nameof(OnNewSaveLoaded));
            return;
        }

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, true);
        TransitionManager.Instance.StartTransitions(this, nameof(LoadSave));
    }

    private void LoadSave()
    {
        SaveHelper.LoadSave(SaveName);
    }

    private void LoadSaveData()
    {
        loadingData = true;

        saveInfoLoadTask = new Task<Save>(() =>
        {
            var save = Save.LoadInfoAndScreenshotFromSave(saveName);

            if (save.Screenshot != null)
            {
                // Rescale the screenshot to save memory etc.
                float aspectRatio = save.Screenshot.GetWidth() / (float)save.Screenshot.GetHeight();

                if (save.Screenshot.GetHeight() > Constants.SAVE_LIST_SCREENSHOT_HEIGHT)
                {
                    // TODO: this seems like a Godot bug, the game crashes often when loading the saves list without
                    // this lock. See: https://github.com/godotengine/godot/issues/55528
                    // Partly resolves: https://github.com/Revolutionary-Games/Thrive/issues/2078
                    // but not for all people and save amounts
                    lock (ResizeLock)
                    {
                        save.Screenshot.Resize((int)(Constants.SAVE_LIST_SCREENSHOT_HEIGHT * aspectRatio),
                            Constants.SAVE_LIST_SCREENSHOT_HEIGHT);
                    }
                }
            }

            return save;
        });

        TaskExecutor.Instance.AddTask(saveInfoLoadTask);
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

        EmitSignal(nameof(OnSelectedChanged));
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

        EmitSignal(nameof(OnDeleted));
    }
}
