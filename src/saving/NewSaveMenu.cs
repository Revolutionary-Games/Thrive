﻿using System;
using System.IO;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;
using Path = System.IO.Path;

/// <summary>
///   Menu for managing making a new save
/// </summary>
public partial class NewSaveMenu : Control
{
#pragma warning disable CA2213
    [Export]
    private SaveList saveList = null!;

    [Export]
    private LineEdit saveNameBox = null!;

    [Export]
    private Button saveButton = null!;

    [Export]
    private CustomConfirmationDialog overwriteConfirm = null!;

    [Export]
    private CustomConfirmationDialog attemptWriteFailAccept = null!;
#pragma warning restore CA2213

    private bool usingSelectedSaveName;

    [Signal]
    public delegate void OnClosedEventHandler();

    [Signal]
    public delegate void OnSaveNameChosenEventHandler(string name);

    public override void _Ready()
    {
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
        overwriteConfirm.DialogText = Localization.Translate("CHOSEN_FILENAME_ALREADY_EXISTS").FormatSafe(name);
        overwriteConfirm.PopupCenteredShrink();
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnClosed);
    }

    private void SaveButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var name = GetSaveName();

        if (FileAccess.FileExists(Path.Combine(Constants.SAVE_FOLDER, name)))
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
        // Verify name is writable
        var name = GetSaveName();
        var path = Path.Combine(Constants.SAVE_FOLDER, name);

        // Make sure the save folder exists, otherwise the write test will always fail
        try
        {
            FileHelpers.MakeSureDirectoryExists(Constants.SAVE_FOLDER);
        }
        catch (IOException e)
        {
            GD.PrintErr("Could not make sure save folder exists for save writability check: ", e);
            attemptWriteFailAccept.PopupCenteredShrink();
            return;
        }

        if (!FileAccess.FileExists(path) && FileHelpers.TryWriteFile(path) != Error.Ok)
        {
            attemptWriteFailAccept.PopupCenteredShrink();
            return;
        }

        EmitSignal(SignalName.OnSaveNameChosen, name);
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
            GUICommon.MarkInputAsValid(saveNameBox);
            saveButton.Disabled = false;
        }
        else
        {
            GUICommon.MarkInputAsInvalid(saveNameBox);
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
            ToolTipManager.Instance.ShowPopup(Localization.Translate("INVALID_SAVE_NAME_POPUP"), 2.5f);
        }
    }

    private void OnSaveListItemConfirmed(SaveListItem item)
    {
        saveNameBox.Text = item.SaveName;
        ShowOverwriteConfirm(item.SaveName);
    }
}
