using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Path = System.IO.Path;

public class ModUploader : Control
{
    [Export]
    public NodePath UploadDialogPath;

    [Export]
    public NodePath ModSelectPath;

    [Export]
    public NodePath UnknownItemActionsPath;

    [Export]
    public NodePath CreateNewButtonPath;

    [Export]
    public NodePath ShowManualEnterIdPath;

    [Export]
    public NodePath ManualIdEntryPath;

    [Export]
    public NodePath AcceptManualIdPath;

    [Export]
    public NodePath ManualEnterIdSectionPath;

    [Export]
    public NodePath DetailsEditorPath;

    [Export]
    public NodePath EditedTitlePath;

    [Export]
    public NodePath EditedDescriptionPath;

    [Export]
    public NodePath EditedVisibilityPath;

    [Export]
    public NodePath EditedTagsPath;

    [Export]
    public NodePath PreviewImageRectPath;

    [Export]
    public NodePath ToBeUploadedContentLocationPath;

    [Export]
    public NodePath ErrorDisplayPath;

    [Export]
    public NodePath FileSelectDialogPath;

    [Export]
    public NodePath WorkshopNoticePath;

    [Export]
    public NodePath UploadSucceededDialogPath;

    [Export]
    public NodePath UploadSucceededTextPath;

    private CustomConfirmationDialog uploadDialog;

    private OptionButton modSelect;

    private Control unknownItemActions;
    private Button createNewButton;

    private Button showManualEnterId;
    private LineEdit manualIdEntry;
    private Button acceptManualId;
    private Control manualEnterIdSection;

    private Control detailsEditor;
    private LineEdit editedTitle;
    private TextEdit editedDescription;
    private CheckBox editedVisibility;
    private LineEdit editedTags;
    private TextureRect previewImageRect;
    private Label toBeUploadedContentLocation;

    private CustomDialog uploadSucceededDialog;
    private CustomRichTextLabel uploadSucceededText;

    private FileDialog fileSelectDialog;

    private CustomRichTextLabel workshopNotice;
    private Label errorDisplay;

    private List<FullModDetails> mods;

    private WorkshopData workshopData;

    private FullModDetails selectedMod;
    private string toBeUploadedPreviewImagePath;

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

        workshopNotice = GetNode<CustomRichTextLabel>(WorkshopNoticePath);
        errorDisplay = GetNode<Label>(ErrorDisplayPath);

        uploadSucceededDialog = GetNode<CustomDialog>(UploadSucceededDialogPath);
        uploadSucceededText = GetNode<CustomRichTextLabel>(UploadSucceededTextPath);

        fileSelectDialog = GetNode<FileDialog>(FileSelectDialogPath);

        fileSelectDialog.Filters = SteamHandler.RecommendedFileEndings.Select(e => "*" + e).ToArray();

        UpdateWorkshopNoticeTexts();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateWorkshopNoticeTexts();
        }

        base._Notification(what);
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

    private void UpdateAvailableModsList()
    {
        modSelect.Clear();

        foreach (var mod in mods)
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

        return workshopData.KnownModWorkshopIds.TryGetValue(selectedMod.InternalName, out _);
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

        editedTitle.Text = selectedMod.Info.Name;
        editedDescription.Text = string.IsNullOrEmpty(selectedMod.Info.LongDescription) ?
            selectedMod.Info.Description :
            selectedMod.Info.LongDescription;
        editedVisibility.Pressed = true;
        editedTags.Text = string.Empty;

        toBeUploadedPreviewImagePath = Path.Combine(selectedMod.Folder, selectedMod.Info.Icon);

        toBeUploadedContentLocation.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("CONTENT_UPLOADED_FROM"), ProjectSettings.GlobalizePath(selectedMod.Folder));

        UpdatePreviewRect();
    }

    private void UpdatePreviewRect()
    {
        if (string.IsNullOrEmpty(toBeUploadedPreviewImagePath))
        {
            previewImageRect.Texture = null;
            return;
        }

        var image = new Image();
        image.Load(toBeUploadedPreviewImagePath);

        var texture = new ImageTexture();
        texture.CreateFromImage(image);

        previewImageRect.Texture = texture;
    }

    /// <summary>
    ///   Checks that all the new info in the upload form is good
    /// </summary>
    /// <returns>True if good, false if not good (also sets the general error message)</returns>
    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(editedTitle.Text))
        {
            SetError(TranslationServer.Translate("MISSING_TITLE"));
            return false;
        }

        if (string.IsNullOrWhiteSpace(editedDescription.Text))
        {
            SetError(TranslationServer.Translate("MISSING_DESCRIPTION"));
            return false;
        }

        // TODO: would be nice to somehow get the Steam constants in here...

        if (editedDescription.Text.Length > 8000)
        {
            SetError(TranslationServer.Translate("DESCRIPTION_TOO_LONG"));
            return false;
        }

        if (editedTags.Text is { Length: > 0 })
        {
            if (string.IsNullOrWhiteSpace(editedTags.Text))
            {
                SetError(TranslationServer.Translate("TAGS_IS_WHITESPACE"));
                return false;
            }

            foreach (var tag in editedTags.Text.Split(','))
            {
                if (!SteamHandler.Tags.Contains(tag))
                {
                    SetError(string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("INVALID_TAG"),
                        tag));
                    return false;
                }
            }
        }

        return true;
    }

    private void ModSelected(int index)
    {
        if (showManualEnterId.Pressed)
            showManualEnterId.Pressed = false;

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

    private void CreateNewPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Create new workshop item button pressed");
        SetProcessingStatus(true);

        SetError(TranslationServer.Translate("CREATING_DOT_DOT_DOT"));

        SteamHandler.Instance.CreateWorkshopItem(result =>
        {
            SetProcessingStatus(false);

            if (!result.Success)
            {
                SetError(result.TranslatedError);
                return;
            }

            GD.Print($"Workshop item create succeeded for \"{selectedMod.InternalName}\", saving the item ID");
            workshopData.KnownModWorkshopIds[selectedMod.InternalName] = result.ItemId;
            try
            {
                workshopData.Save();
            }
            catch (Exception e)
            {
                GD.PrintErr("Saving workshop data failed: ", e);
                SetError(string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("SAVING_DATA_FAILED_DUE_TO"),
                    e.Message));
                return;
            }

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

        var updateData = new WorkshopItemData
        {
            Id = workshopData.KnownModWorkshopIds[selectedMod.InternalName],
            Title = editedTitle.Text,
            Description = editedDescription.Text,
            Visibility = editedVisibility.Pressed ? SteamItemVisibility.Public : SteamItemVisibility.Private,
            ContentFolder = ProjectSettings.GlobalizePath(selectedMod.Folder),
            PreviewImagePath = ProjectSettings.GlobalizePath(toBeUploadedPreviewImagePath),
        };

        if (!string.IsNullOrWhiteSpace(editedTags.Text))
        {
            updateData.Tags = editedTags.Text.Split(',').ToList();
            GD.Print("Setting item tags: ", string.Join(", ", updateData.Tags));
        }

        // TODO: proper progress bar
        SetError(TranslationServer.Translate("UPLOADING_DOT_DOT_DOT"));

        // TODO: implement change notes text input
        SteamHandler.Instance.UpdateWorkshopItem(updateData, null, result =>
        {
            SetProcessingStatus(false);

            // TODO: save the details in workshopData so that the uploaded info can be pre-filled when
            // uploading an update

            if (!result.Success)
            {
                SetError(result.TranslatedError);
                return;
            }

            uploadedItemId = updateData.Id;

            GD.Print($"Workshop item updated for \"{selectedMod.InternalName}\"");

            ClearError();
            uploadDialog.Hide();

            // Doesn't work inside Translate method calls for text extraction
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (result.TermsOfServiceSigningRequired)
            {
                uploadSucceededText.ExtendedBbcode =
                    TranslationServer.Translate("WORKSHOP_ITEM_UPLOAD_SUCCEEDED_TOS_REQUIRED");
            }
            else
            {
                uploadSucceededText.ExtendedBbcode = TranslationServer.Translate("WORKSHOP_ITEM_UPLOAD_SUCCEEDED");
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

        fileSelectDialog.DeselectItems();
        fileSelectDialog.CurrentDir = "user://";
        fileSelectDialog.CurrentPath = "user://";
        fileSelectDialog.PopupCenteredClamped(new Vector2(700, 400));
    }

    private void OnFileSelected(string selected)
    {
        if (selected == null)
        {
            toBeUploadedPreviewImagePath = null;
            UpdatePreviewRect();
            return;
        }

        using var file = new File();

        if (!file.FileExists(selected))
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
        workshopNotice.ExtendedBbcode = TranslationServer.Translate("WORKSHOP_TERMS_OF_SERVICE_NOTICE");
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

    private void SetError(string message)
    {
        if (message == null)
        {
            ClearError();
        }

        errorDisplay.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("FORM_ERROR_MESSAGE"),
            message);
    }

    private void ClearError()
    {
        errorDisplay.Text = string.Empty;
    }
}
