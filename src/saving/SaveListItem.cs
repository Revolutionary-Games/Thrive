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
    public NodePath SelectedPath;

    [Export]
    public NodePath LoadButtonPath;

    private Label saveNameLabel;
    private TextureRect screenshot;
    private Label version;
    private Label versionWarning;
    private Label type;
    private Label createdAt;
    private Label createdBy;
    private Label createdOnPlatform;
    private Label description;
    private CheckBox selected;
    private Button loadButton;

    private string saveName;
    private bool compatibleVersion;

    private bool loadingData;
    private Task<Save> saveInfoLoadTask;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnDeleted();

    [Signal]
    public delegate void OnOldSaveLoaded();

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

    public bool Selected
    {
        get
        {
            if (!Selectable)
                return false;

            return selected.Pressed;
        }
        set
        {
            if (!Selectable)
                throw new InvalidOperationException();

            selected.Pressed = value;
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
        selected = GetNode<CheckBox>(SelectedPath);
        loadButton = GetNode<Button>(LoadButtonPath);

        selected.Visible = Selectable;

        loadButton.Visible = Loadable;

        UpdateName();
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

        // Screenshot
        var texture = new ImageTexture();
        texture.CreateFromImage(save.Screenshot);

        screenshot.Texture = texture;

        // General info
        compatibleVersion = save.Info.ThriveVersion == Constants.Version;

        version.Text = save.Info.ThriveVersion;
        versionWarning.Visible = !compatibleVersion;
        type.Text = save.Info.Type.ToString();
        createdAt.Text = save.Info.CreatedAt.ToString("G", CultureInfo.CurrentCulture);
        createdBy.Text = save.Info.Creator;
        createdOnPlatform.Text = save.Info.Platform;
        description.Text = save.Info.Description;

        loadingData = false;
    }

    public void LoadThisSave()
    {
        if (compatibleVersion)
        {
            SaveHelper.LoadSave(SaveName);
            return;
        }

        EmitSignal(nameof(OnOldSaveLoaded));
    }

    private void LoadSaveData()
    {
        loadingData = true;

        saveInfoLoadTask = new Task<Save>(() =>
        {
            var save = Save.LoadInfoAndScreenshotFromSave(saveName);

            // Rescale the screenshot to save memory etc.
            float aspectRatio = save.Screenshot.GetWidth() / (float)save.Screenshot.GetHeight();

            if (save.Screenshot.GetHeight() > Constants.SAVE_LIST_SCREENSHOT_HEIGHT)
            {
                save.Screenshot.Resize((int)(Constants.SAVE_LIST_SCREENSHOT_HEIGHT * aspectRatio),
                    Constants.SAVE_LIST_SCREENSHOT_HEIGHT);
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

    private void OnSelectedCheckboxChanged(bool newValue)
    {
        _ = newValue;
        EmitSignal(nameof(OnSelectedChanged));
    }

    private void DeletePressed()
    {
        EmitSignal(nameof(OnDeleted));
    }
}
