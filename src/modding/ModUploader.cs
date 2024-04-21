using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Path = System.IO.Path;

/// <summary>
///   GUI for allowing the player to upload a mod
/// </summary>
public partial class ModUploader : Control
{
    [Export]
    public NodePath? UploadDialogPath;

    [Export]
    public NodePath ModSelectPath = null!;

    [Export]
    public NodePath UnknownItemActionsPath = null!;

    [Export]
    public NodePath CreateNewButtonPath = null!;

    [Export]
    public NodePath ShowManualEnterIdPath = null!;

    [Export]
    public NodePath ManualIdEntryPath = null!;

    [Export]
    public NodePath AcceptManualIdPath = null!;

    [Export]
    public NodePath ManualEnterIdSectionPath = null!;

    [Export]
    public NodePath DetailsEditorPath = null!;

    [Export]
    public NodePath EditedTitlePath = null!;

    [Export]
    public NodePath EditedDescriptionPath = null!;

    [Export]
    public NodePath EditedVisibilityPath = null!;

    [Export]
    public NodePath EditedTagsPath = null!;

    [Export]
    public NodePath PreviewImageRectPath = null!;

    [Export]
    public NodePath ToBeUploadedContentLocationPath = null!;

    [Export]
    public NodePath ErrorDisplayPath = null!;

    [Export]
    public NodePath FileSelectDialogPath = null!;

    [Export]
    public NodePath WorkshopNoticePath = null!;

    [Export]
    public NodePath ChangeNotesPath = null!;

    [Export]
    public NodePath UploadSucceededDialogPath = null!;

    [Export]
    public NodePath UploadSucceededTextPath = null!;

#pragma warning disable CA2213
    private CustomConfirmationDialog uploadDialog = null!;

    private OptionButton modSelect = null!;

    private Control unknownItemActions = null!;
    private Button createNewButton = null!;

    private Button showManualEnterId = null!;
    private LineEdit manualIdEntry = null!;
    private Button acceptManualId = null!;
    private Control manualEnterIdSection = null!;

    private Control detailsEditor = null!;
    private LineEdit editedTitle = null!;
    private TextEdit editedDescription = null!;
    private CheckBox editedVisibility = null!;
    private LineEdit editedTags = null!;
    private TextureRect previewImageRect = null!;
    private Label toBeUploadedContentLocation = null!;
    private TextEdit changeNotes = null!;

    private CustomWindow uploadSucceededDialog = null!;
    private CustomRichTextLabel uploadSucceededText = null!;

    private FileDialog fileSelectDialog = null!;

    private CustomRichTextLabel workshopNotice = null!;
    private Label errorDisplay = null!;
#pragma warning restore CA2213

    private List<FullModDetails>? mods;

    private WorkshopData? workshopData;

    private FullModDetails? selectedMod;
    private string? toBeUploadedPreviewImagePath;

    private bool manualEnterWorkshopId;
    private bool processing;

    private ulong uploadedItemId;

    public override void _Ready()
    {
        uploadDialog = GetNode<CustomConfirmationDialog>(UploadDialogPath);

        modSelect = GetNode<OptionButton>(ModSelectPath);

        unknownItemActions = GetNode<Control>(UnknownItemActionsPath);
        createNewButton = GetNode<Button>(CreateNewButtonPath);
        showManualEnterId = GetNode<Button>(ShowManualEnterIdPath);
        manualIdEntry = GetNode<LineEdit>(ManualIdEntryPath);
        acceptManualId = GetNode<Button>(AcceptManualIdPath);
        manualEnterIdSection = GetNode<Control>(ManualEnterIdSectionPath);

        detailsEditor = GetNode<Control>(DetailsEditorPath);
        editedTitle = GetNode<LineEdit>(EditedTitlePath);
        editedDescription = GetNode<TextEdit>(EditedDescriptionPath);
        editedVisibility = GetNode<CheckBox>(EditedVisibilityPath);
        editedTags = GetNode<LineEdit>(EditedTagsPath);
        previewImageRect = GetNode<TextureRect>(PreviewImageRectPath);
        toBeUploadedContentLocation = GetNode<Label>(ToBeUploadedContentLocationPath);
        changeNotes = GetNode<TextEdit>(ChangeNotesPath);

        workshopNotice = GetNode<CustomRichTextLabel>(WorkshopNoticePath);
        errorDisplay = GetNode<Label>(ErrorDisplayPath);

        uploadSucceededDialog = GetNode<CustomWindow>(UploadSucceededDialogPath);
        uploadSucceededText = GetNode<CustomRichTextLabel>(UploadSucceededTextPath);

        fileSelectDialog = GetNode<FileDialog>(FileSelectDialogPath);

        fileSelectDialog.Filters = SteamHandler.RecommendedFileEndings.Select(e => "*" + e).ToArray();

        UpdateWorkshopNoticeTexts();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += UpdateWorkshopNoticeTexts;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= UpdateWorkshopNoticeTexts;
    }

    public void Open(IEnumerable<FullModDetails> availableMods)
    {
        workshopData = WorkshopData.Load();

        mods = availableMods.ToList();

        UpdateAvailableModsList();
        UpdateLayout();

        uploadDialog.PopupCenteredShrink();
        UpdateUploadButtonStatus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (UploadDialogPath != null)
            {
                UploadDialogPath.Dispose();
                ModSelectPath.Dispose();
                UnknownItemActionsPath.Dispose();
                CreateNewButtonPath.Dispose();
                ShowManualEnterIdPath.Dispose();
                ManualIdEntryPath.Dispose();
                AcceptManualIdPath.Dispose();
                ManualEnterIdSectionPath.Dispose();
                DetailsEditorPath.Dispose();
                EditedTitlePath.Dispose();
                EditedDescriptionPath.Dispose();
                EditedVisibilityPath.Dispose();
                EditedTagsPath.Dispose();
                PreviewImageRectPath.Dispose();
                ToBeUploadedContentLocationPath.Dispose();
                ErrorDisplayPath.Dispose();
                FileSelectDialogPath.Dispose();
                WorkshopNoticePath.Dispose();
                ChangeNotesPath.Dispose();
                UploadSucceededDialogPath.Dispose();
                UploadSucceededTextPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateAvailableModsList()
    {
        modSelect.Clear();

        foreach (var mod in mods!)
        {
            // When clicking the OptionButton the opened list doesn't scale down the icon sizes so we can't use icons
            // TODO: report the above bug to Godot and get this fixed
            // modSelect.AddIconItem(ModManager.LoadModIcon(mod), mod.InternalName);
            modSelect.AddItem(mod.InternalName);
        }

        if (modSelect.Selected != -1)
        {
            ModSelected(modSelect.Selected);
        }
    }

    private bool SelectedModHasItemId()
    {
        if (selectedMod == null)
            return false;

        return workshopData!.KnownModWorkshopIds.TryGetValue(selectedMod.InternalName, out _);
    }

    private void UpdateLayout()
    {
        if (selectedMod == null)
        {
            unknownItemActions.Visible = false;
            detailsEditor.Visible = false;
            return;
        }

        if (!SelectedModHasItemId())
        {
            unknownItemActions.Visible = true;
            detailsEditor.Visible = false;

            manualEnterIdSection.Visible = manualEnterWorkshopId;

            return;
        }

        unknownItemActions.Visible = false;
        detailsEditor.Visible = true;
    }

    private void UpdateUploadButtonStatus()
    {
        if (!SelectedModHasItemId() || processing)
        {
            uploadDialog.SetConfirmDisabled(true);
        }
        else
        {
            uploadDialog.SetConfirmDisabled(false);
        }
    }

    private void UpdateModDetails()
    {
        if (selectedMod == null)
            return;

        if (workshopData!.PreviouslyUploadedItemData.TryGetValue(selectedMod.InternalName, out var previousData))
        {
            editedTitle.Text = previousData.Title;
            editedDescription.Text = previousData.Description;
            editedVisibility.ButtonPressed = previousData.Visibility == SteamItemVisibility.Public;
            editedTags.Text = string.Join(",", previousData.Tags);

            toBeUploadedPreviewImagePath = previousData.PreviewImagePath;

            changeNotes.Text = string.Empty;

            ValidateForm();
        }
        else
        {
            editedTitle.Text = selectedMod.Info.Name;
            editedDescription.Text = string.IsNullOrEmpty(selectedMod.Info.LongDescription) ?
                selectedMod.Info.Description :
                selectedMod.Info.LongDescription;
            editedVisibility.ButtonPressed = true;
            editedTags.Text = string.Empty;

            if (selectedMod.Info.Icon == null)
            {
                toBeUploadedPreviewImagePath = null;
            }
            else
            {
                toBeUploadedPreviewImagePath = Path.Combine(selectedMod.Folder, selectedMod.Info.Icon);
            }

            // TODO: this is not translated here as the default language to upload mods in, is English
            // See: https://github.com/Revolutionary-Games/Thrive/issues/2828
            changeNotes.Text = "Initial version";
        }

        toBeUploadedContentLocation.Text = Localization.Translate("CONTENT_UPLOADED_FROM")
            .FormatSafe(ProjectSettings.GlobalizePath(selectedMod.Folder));

        UpdatePreviewRect();
    }

    private void UpdatePreviewRect()
    {
        if (string.IsNullOrEmpty(toBeUploadedPreviewImagePath))
        {
            previewImageRect.Texture = null;
            return;
        }

        var image = Image.LoadFromFile(toBeUploadedPreviewImagePath);

        previewImageRect.Texture = ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    ///   Checks that all the new info in the upload form is good
    /// </summary>
    /// <returns>True if good, false if not good (also sets the general error message)</returns>
    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(editedTitle.Text))
        {
            SetError(Localization.Translate("MISSING_TITLE"));
            return false;
        }

        if (string.IsNullOrWhiteSpace(editedDescription.Text))
        {
            SetError(Localization.Translate("MISSING_DESCRIPTION"));
            return false;
        }

        // TODO: would be nice to somehow get the Steam constants in here...

        if (editedDescription.Text.Length > 8000)
        {
            SetError(Localization.Translate("DESCRIPTION_TOO_LONG"));
            return false;
        }

        if (changeNotes.Text.Length > 8000)
        {
            SetError(Localization.Translate("CHANGE_DESCRIPTION_IS_TOO_LONG"));
            return false;
        }

        if (editedTags.Text is { Length: > 0 })
        {
            if (string.IsNullOrWhiteSpace(editedTags.Text))
            {
                SetError(Localization.Translate("TAGS_IS_WHITESPACE"));
                return false;
            }

            foreach (var tag in editedTags.Text.Split(','))
            {
                if (!SteamHandler.Tags.Contains(tag))
                {
                    SetError(Localization.Translate("INVALID_TAG").FormatSafe(tag));
                    return false;
                }
            }
        }

        if (!string.IsNullOrEmpty(toBeUploadedPreviewImagePath))
        {
            using var file = FileAccess.Open(toBeUploadedPreviewImagePath, FileAccess.ModeFlags.Read);

            if (file == null)
            {
                SetError(Localization.Translate("PREVIEW_IMAGE_DOES_NOT_EXIST"));
                return false;
            }

            // Let's hope Steam uses megabytes and not mebibytes as the limit
            if (file.GetLength() >= 1000000)
            {
                SetError(Localization.Translate("PREVIEW_IMAGE_IS_TOO_LARGE"));
                return false;
            }
        }

        return true;
    }

    private void ModSelected(int index)
    {
        if (mods == null)
        {
            GD.PrintErr("Mod selected but we haven't been initialized");
            return;
        }

        if (showManualEnterId.ButtonPressed)
            showManualEnterId.ButtonPressed = false;

        if (index == -1)
        {
            selectedMod = null;
            UpdateLayout();
            return;
        }

        var name = modSelect.GetItemText(index);

        selectedMod = mods.FirstOrDefault(m => m.InternalName == name);

        ClearError();
        UpdateLayout();
        UpdateModDetails();
        UpdateUploadButtonStatus();
    }

    private void SelectManualIdEnterMode(bool selected)
    {
        // TODO: play sound so that when this is reset it doesn't play
        // GUICommon.Instance.PlayButtonPressSound();

        manualEnterWorkshopId = selected;
        UpdateLayout();
    }

    private void OnManualIdEntered()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("Attempted to set ID for selected mod, but there is no selected mod");
            return;
        }

        if (!ulong.TryParse(manualIdEntry.Text, out ulong id))
        {
            SetError(Localization.Translate("ID_IS_NOT_A_NUMBER"));
            return;
        }

        GD.Print($"Workshop item id manually set for \"{selectedMod.InternalName}\", to: ", id);
        workshopData!.KnownModWorkshopIds[selectedMod.InternalName] = id;

        ClearError();
        UpdateLayout();
        UpdateUploadButtonStatus();
    }

    private void OnForgetDataPressed()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("Can't forget a null mod");
            return;
        }

        GD.Print("Forgetting local data about workshop mod: ", selectedMod.InternalName);

        workshopData!.RemoveDataForMod(selectedMod.InternalName);

        if (!SaveWorkshopData())
            return;

        ModSelected(modSelect.Selected);
    }

    private void CreateNewPressed()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("Can't create a mod without any being selected");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Create new workshop item button pressed");
        SetProcessingStatus(true);

        errorDisplay.Text = Localization.Translate("CREATING_DOT_DOT_DOT");

        SteamHandler.Instance.CreateWorkshopItem(result =>
        {
            SetProcessingStatus(false);

            if (!result.Success)
            {
                SetError(result.TranslatedError);
                return;
            }

            if (result.ItemId == null)
            {
                SetError(Localization.Translate("SUCCESS_BUT_MISSING_ID"));
                return;
            }

            GD.Print($"Workshop item create succeeded for \"{selectedMod.InternalName}\", saving the item ID");
            workshopData!.KnownModWorkshopIds[selectedMod.InternalName] = result.ItemId.Value;

            if (!SaveWorkshopData())
                return;

            ClearError();
            UpdateLayout();
            UpdateUploadButtonStatus();
        });
    }

    private void UploadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!ValidateForm())
        {
            GD.PrintErr("Invalid data, not starting upload");
            return;
        }

        GD.Print("Form validation passed, starting mod upload");

        SetProcessingStatus(true);

        var updateData = new WorkshopItemData(workshopData!.KnownModWorkshopIds[selectedMod!.InternalName],
            editedTitle.Text, ProjectSettings.GlobalizePath(selectedMod.Folder),
            ProjectSettings.GlobalizePath(toBeUploadedPreviewImagePath))
        {
            Description = editedDescription.Text,
            Visibility = editedVisibility.ButtonPressed ? SteamItemVisibility.Public : SteamItemVisibility.Private,
        };

        if (!string.IsNullOrWhiteSpace(editedTags.Text))
        {
            updateData.Tags = editedTags.Text.Split(',').ToList();
            GD.Print("Setting item tags: ", string.Join(", ", updateData.Tags));
        }

        // TODO: proper progress bar
        errorDisplay.Text = Localization.Translate("UPLOADING_DOT_DOT_DOT");

        string? notes = null;

        if (!string.IsNullOrWhiteSpace(changeNotes.Text))
        {
            notes = changeNotes.Text;
        }

        SteamHandler.Instance.UpdateWorkshopItem(updateData, notes, result =>
        {
            SetProcessingStatus(false);

            if (!result.Success)
            {
                SetError(result.TranslatedError);
                return;
            }

            uploadedItemId = updateData.Id;

            GD.Print($"Workshop item updated for \"{selectedMod.InternalName}\"");

            // Save the details in workshopData so that the uploaded info can be pre-filled when uploading an update
            workshopData.PreviouslyUploadedItemData[selectedMod.InternalName] = updateData;

            if (!SaveWorkshopData())
                return;

            ClearError();
            uploadDialog.Hide();

            // Doesn't work inside Translate method calls for text extraction
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (result.TermsOfServiceSigningRequired)
            {
                uploadSucceededText.ExtendedBbcode =
                    Localization.Translate("WORKSHOP_ITEM_UPLOAD_SUCCEEDED_TOS_REQUIRED");
            }
            else
            {
                uploadSucceededText.ExtendedBbcode = Localization.Translate("WORKSHOP_ITEM_UPLOAD_SUCCEEDED");
            }

            uploadSucceededDialog.PopupCenteredShrink();
        });
    }

    private void SetProcessingStatus(bool newStatus)
    {
        processing = newStatus;
        UpdateButtonDisabledStates();
    }

    private void BrowseForPreviewImage()
    {
        GUICommon.Instance.PlayButtonPressSound();

        fileSelectDialog.DeselectAll();
        fileSelectDialog.CurrentDir = "user://";
        fileSelectDialog.CurrentPath = "user://";
        fileSelectDialog.PopupCenteredClamped(new Vector2I(700, 400));
    }

    private void OnFileSelected(string? selected)
    {
        if (selected == null)
        {
            toBeUploadedPreviewImagePath = null;
            UpdatePreviewRect();
            return;
        }

        if (!FileAccess.FileExists(selected))
        {
            GD.PrintErr("Selected preview image file doesn't exist");
            return;
        }

        toBeUploadedPreviewImagePath = selected;
        UpdatePreviewRect();
    }

    private void UpdateButtonDisabledStates()
    {
        modSelect.Disabled = processing;

        showManualEnterId.Disabled = processing;
        createNewButton.Disabled = processing;

        manualIdEntry.Editable = !processing;
        acceptManualId.Disabled = processing;

        UpdateUploadButtonStatus();
    }

    private void UpdateWorkshopNoticeTexts()
    {
        // Rich text labels don't seem to automatically translate their text, so we do it for the label here
        workshopNotice.ExtendedBbcode = Localization.Translate("WORKSHOP_TERMS_OF_SERVICE_NOTICE");
    }

    private void DismissSuccessDialog()
    {
        GUICommon.Instance.PlayButtonPressSound();
        uploadSucceededDialog.Hide();
    }

    private void SuccessDialogClosed()
    {
        // TODO: add a settings option to disable this
        SteamHandler.Instance.OpenWorkshopItemInOverlayBrowser(uploadedItemId);
    }

    private bool SaveWorkshopData()
    {
        try
        {
            workshopData!.Save();
        }
        catch (Exception e)
        {
            GD.PrintErr("Saving workshop data failed: ", e);
            SetError(Localization.Translate("SAVING_DATA_FAILED_DUE_TO").FormatSafe(e.Message));
            return false;
        }

        return true;
    }

    private void SetError(string? message)
    {
        if (message == null)
        {
            ClearError();
        }

        errorDisplay.Text = Localization.Translate("FORM_ERROR_MESSAGE").FormatSafe(message);
    }

    private void ClearError()
    {
        errorDisplay.Text = string.Empty;
    }
}
