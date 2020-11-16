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
    public NodePath SaveNamePath;

    [Export]
    public NodePath ScreenshotPath;

    [Export]
    public NodePath VersionPath;

    [Export]
    public NodePath VersionWarningPath;

    [Export]
    public NodePath TypePath;

    [Export]
    public NodePath CreatedAtPath;

    [Export]
    public NodePath CreatedByPath;

    [Export]
    public NodePath CreatedOnPlatformPath;

    [Export]
    public NodePath DescriptionPath;

    [Export]
    public NodePath LoadButtonPath;

    [Export]
    public NodePath HighlightPath;

    private Label saveNameLabel;
    private TextureRect screenshot;
    private Label version;
    private Label versionWarning;
    private Label type;
    private Label createdAt;
    private Label createdBy;
    private Label createdOnPlatform;
    private Label description;
    private Button loadButton;
    private Panel highlightPanel;

    private string saveName;
    private int versionDifference;

    private bool loadingData;
    private Task<Save> saveInfoLoadTask;

    private bool highlighted;
    private bool selected;

    private bool isBroken;
    private bool isKnownIncompatible;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnDeleted();

    [Signal]
    public delegate void OnOldSaveLoaded();

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

        if (!saveInfoLoadTask.IsCompleted)
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
            if (SaveHelper.IsKnownIncompatible(save.Info.ThriveVersion))
                isKnownIncompatible = true;
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
        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.Pressed && mouse.ButtonIndex == (int)ButtonList.Left)
            {
                OnSelect();
                AcceptEvent();
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

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, true);
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
                    save.Screenshot.Resize((int)(Constants.SAVE_LIST_SCREENSHOT_HEIGHT * aspectRatio),
                        Constants.SAVE_LIST_SCREENSHOT_HEIGHT);
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
