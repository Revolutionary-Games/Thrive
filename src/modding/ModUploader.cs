using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

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
    public NodePath ErrorDisplayPath;

    [Export]
    public NodePath FileSelectDialogPath;

    [Export]
    public NodePath WorkshopNoticePath;

    private CustomConfirmationDialog uploadDialog;

    private OptionButton modSelect;

    private Control unknownItemActions;
    private Button createNewButton;

    private Button showManualEnterId;
    private LineEdit manualIdEntry;
    private Button acceptManualId;
    private Control manualEnterIdSection;

    private Control detailsEditor;

    private FileDialog fileSelectDialog;

    private CustomRichTextLabel workshopNotice;
    private Label errorDisplay;

    private List<FullModDetails> mods;

    private WorkshopData workshopData;

    private FullModDetails selectedMod;

    private bool manualEnterWorkshopId;
    private bool processing;

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

        workshopNotice = GetNode<CustomRichTextLabel>(WorkshopNoticePath);
        errorDisplay = GetNode<Label>(ErrorDisplayPath);

        fileSelectDialog = GetNode<FileDialog>(FileSelectDialogPath);

        UpdateWorkshopNoticeTexts();
    }

    /*public override void _Process(float delta)
    {
        if (!uploadDialog.Visible)
            return;
    }*/

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateWorkshopNoticeTexts();
        }

        base._Notification(what);
    }

    public void Open(List<FullModDetails> availableMods)
    {
        workshopData = WorkshopData.Load();

        mods = availableMods;

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

    private void UpdateLayout()
    {
        if (selectedMod == null)
        {
            unknownItemActions.Visible = false;
            detailsEditor.Visible = false;
            return;
        }

        if (!workshopData.KnownModWorkshopIds.TryGetValue(selectedMod.InternalName, out _))
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
        if (selectedMod == null || processing)
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

        UpdateLayout();
        UpdateModDetails();
    }

    private void SelectManualIdEnterMode(bool selected)
    {
        manualEnterWorkshopId = selected;
        UpdateLayout();
    }

    private void CreateNewPressed()
    {
        GD.Print("Create new workshop item button pressed");
        SetProcessingStatus(false);

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
            UpdateModDetails();
        });
    }

    private void SetProcessingStatus(bool newStatus)
    {
        processing = newStatus;
        UpdateButtonDisabledStates();
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
