using System;
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
    private TextEdit saveNameBox;
    private ConfirmationDialog overwriteConfirm;

    private bool usingSelectedSaveName;

    [Signal]
    public delegate void OnClosed();

    [Signal]
    public delegate void OnSaveNameChosen(string name);

    public override void _Ready()
    {
        saveList = GetNode<SaveList>(SaveListPath);
        saveNameBox = GetNode<TextEdit>(SaveNameBoxPath);
        overwriteConfirm = GetNode<ConfirmationDialog>(OverwriteConfirmPath);
    }

    private void ClosePressed()
    {
        EmitSignal(nameof(OnClosed));
    }

    private void SaveButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var name = GetSaveName();

        if (FileHelpers.Exists(PathUtils.Join(Constants.SAVE_FOLDER, name)))
        {
            overwriteConfirm.DialogText = $"The chosen filename ({name}) already exists. Overwrite?";
            overwriteConfirm.PopupCentered();
        }
        else
        {
            OnConfirmSaveName();
        }
    }

    private void OnConfirmSaveName()
    {
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
}
