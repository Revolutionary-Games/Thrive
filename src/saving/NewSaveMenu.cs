using System;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Menu for managing making a new save
/// </summary>
public class NewSaveMenu : Control
{
    [Export]
    public NodePath SaveListPath = null!;

    [Export]
    public NodePath SaveNameBoxPath = null!;

    [Export]
    public NodePath OverwriteConfirmPath = null!;

    [Export]
    public NodePath SaveButtonPath = null!;

    private SaveList saveList = null!;
    private LineEdit saveNameBox = null!;
    private Button saveButton = null!;
    private CustomConfirmationDialog overwriteConfirm = null!;

    private bool usingSelectedSaveName;

    [Signal]
    public delegate void OnClosed();

    [Signal]
    public delegate void OnSaveNameChosen(string name);

    public override void _Ready()
    {
        saveList = GetNode<SaveList>(SaveListPath);
        saveNameBox = GetNode<LineEdit>(SaveNameBoxPath);
        saveButton = GetNode<Button>(SaveButtonPath);
        overwriteConfirm = GetNode<CustomConfirmationDialog>(OverwriteConfirmPath);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && Visible)
        {
            saveNameBox.GrabFocus();
        }
    }

    public void RefreshExisting()
    {
        saveList.Refresh();
    }

    public void SetSaveName(string name, bool selectText = false)
    {
        saveNameBox.Text = name;

        if (selectText)
            saveNameBox.SelectAll();
    }

    private static bool IsSaveNameValid(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && !name.Any(Constants.FILE_NAME_DISALLOWED_CHARACTERS.Contains);
    }

    private void ShowOverwriteConfirm(string name)
    {
        // The chosen filename ({0}) already exists. Overwrite?
        overwriteConfirm.DialogText = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("THE_CHOSEN_FILENAME_ALREADY_EXISTS"),
            name);
        overwriteConfirm.PopupCenteredShrink();
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnClosed));
    }

    private void SaveButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var name = GetSaveName();

        if (FileHelpers.Exists(PathUtils.Join(Constants.SAVE_FOLDER, name)))
        {
            ShowOverwriteConfirm(name);
        }
        else
        {
            OnConfirmSaveName();
        }
    }

    private void OnConfirmSaveName()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnSaveNameChosen), GetSaveName());
    }

    private string GetSaveName()
    {
        var name = saveNameBox.Text;

        // Make sure ends with the extension
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (!name.EndsWith(Constants.SAVE_EXTENSION_WITH_DOT, StringComparison.Ordinal))
                name += Constants.SAVE_EXTENSION_WITH_DOT;
        }

        return name;
    }

    private void OnSelectedChanged()
    {
        var selected = saveList.GetSelectedItems().ToList();
        if (selected.Count < 1)
        {
            if (usingSelectedSaveName)
            {
                saveNameBox.Text = string.Empty;
                usingSelectedSaveName = false;
            }

            return;
        }

        // Deselect all except the last one
        if (selected.Count > 1)
        {
            for (int i = 0; i < selected.Count - 1; ++i)
            {
                selected[i].Selected = false;
            }
        }

        saveNameBox.Text = selected.Last().SaveName.Replace(Constants.SAVE_EXTENSION_WITH_DOT, string.Empty);
        usingSelectedSaveName = true;
    }

    private void OnSaveNameTextChanged(string newName)
    {
        if (IsSaveNameValid(newName))
        {
            saveNameBox.Set("custom_colors/font_color", new Color(1, 1, 1));
            saveButton.Disabled = false;
        }
        else
        {
            saveNameBox.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
            saveButton.Disabled = true;
        }
    }

    private void OnSaveNameTextEntered(string newName)
    {
        if (IsSaveNameValid(newName))
        {
            SaveButtonPressed();
        }
        else
        {
            ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("INVALID_SAVE_NAME_POPUP"), 2.5f);
        }
    }

    private void OnSaveListItemConfirmed(SaveListItem item)
    {
        saveNameBox.Text = item.SaveName;
        ShowOverwriteConfirm(item.SaveName);
    }
}
