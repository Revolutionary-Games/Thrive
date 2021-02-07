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
    public NodePath SaveListPath;

    [Export]
    public NodePath SaveNameBoxPath;

    [Export]
    public NodePath OverwriteConfirmPath;

    private SaveList saveList;
    private LineEdit saveNameBox;
    private ConfirmationDialog overwriteConfirm;

    private bool usingSelectedSaveName;

    [Signal]
    public delegate void OnClosed();

    [Signal]
    public delegate void OnSaveNameChosen(string name);

    public override void _Ready()
    {
        saveList = GetNode<SaveList>(SaveListPath);
        saveNameBox = GetNode<LineEdit>(SaveNameBoxPath);
        overwriteConfirm = GetNode<ConfirmationDialog>(OverwriteConfirmPath);
    }

    public void RefreshExisting()
    {
        saveList.Refresh();
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
            // The chosen filename ({0}) already exists. Overwrite?
            overwriteConfirm.GetNode<Label>("DialogText").Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("THE_CHOSEN_FILENAME_ALREADY_EXISTS"),
                name);
            overwriteConfirm.PopupCenteredShrink();
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

    private void OnSaveNameTextEntered(string newName)
    {
        _ = newName;

        SaveButtonPressed();
    }
}
