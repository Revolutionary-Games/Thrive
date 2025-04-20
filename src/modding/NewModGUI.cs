using System;
using System.IO;
using Godot;
using Newtonsoft.Json;
using Path = System.IO.Path;

/// <summary>
///   GUI for setting up the basic structure of a mod (when done creates the folder and files for the new mod)
/// </summary>
public partial class NewModGUI : Control
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow dialog = null!;

    [Export]
    private LineEdit internalName = null!;
    [Export]
    private LineEdit name = null!;
    [Export]
    private LineEdit author = null!;
    [Export]
    private LineEdit version = null!;
    [Export]
    private LineEdit description = null!;
    [Export]
    private TextEdit longDescription = null!;
    [Export]
    private LineEdit iconFile = null!;
    [Export]
    private LineEdit infoUrl = null!;
    [Export]
    private LineEdit license = null!;
    [Export]
    private LineEdit recommendedThrive = null!;
    [Export]
    private LineEdit minimumThrive = null!;
    [Export]
    private LineEdit maximumThrive = null!;
    [Export]
    private LineEdit pckName = null!;
    [Export]
    private LineEdit modAssembly = null!;
    [Export]
    private LineEdit assemblyModClass = null!;
    [Export]
    private CheckBox assemblyModAutoHarmony = null!;

    [Export]
    private Label errorDisplay = null!;
#pragma warning restore CA2213

    private ModInfo? editedInfo;

    [Signal]
    public delegate void OnCanceledEventHandler();

    /// <summary>
    ///   Emitted when creation is accepted. Contains the full JSON serialized <see cref="FullModDetails"/> object.
    /// </summary>
    [Signal]
    public delegate void OnAcceptedEventHandler(string newModInfo);

    public override void _Ready()
    {
    }

    public void Open()
    {
        ResetForm();

        dialog.PopupCenteredShrink();
    }

    private void Closed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnCanceled);
    }

    private void Cancel()
    {
        dialog.Hide();
    }

    private void Create()
    {
        if (editedInfo == null)
        {
            GD.PrintErr("Create called with edited info being null");
            return;
        }

        if (!ReadControlsToEditedInfo())
        {
            GUICommon.Instance.PlayButtonPressSound();
            return;
        }

        var serialized = ValidateFormData();

        if (serialized == null)
        {
            GUICommon.Instance.PlayButtonPressSound();
            return;
        }

        ClearError();

        dialog.Hide();
        EmitSignal(SignalName.OnAccepted, serialized);
    }

    private void WikiLinkPressed()
    {
        if (OS.ShellOpen("https://wiki.revolutionarygamesstudio.com/wiki/Modding") != Error.Ok)
        {
            GD.PrintErr("Failed to open modding guide wiki page");
        }
    }

    private void ResetForm()
    {
        editedInfo = new ModInfo
        {
            Author = Settings.EnvironmentUserName,
            Version = "1.0",
            Description = Localization.Translate("NEW_MOD_DEFAULT_DESCRIPTION"),
            License = "proprietary",
            RecommendedThriveVersion = Constants.Version,
            MinimumThriveVersion = Constants.Version,
        };

        ApplyEditedInfoToControls();
    }

    private void ApplyEditedInfoToControls()
    {
        name.Text = editedInfo!.Name;
        internalName.Text = editedInfo.InternalName;
        author.Text = editedInfo.Author;
        version.Text = editedInfo.Version;
        description.Text = editedInfo.Description;
        longDescription.Text = editedInfo.LongDescription;
        iconFile.Text = editedInfo.Icon;
        infoUrl.Text = editedInfo.InfoUrl == null ? string.Empty : editedInfo.InfoUrl.ToString();
        license.Text = editedInfo.License;
        recommendedThrive.Text = editedInfo.RecommendedThriveVersion;
        minimumThrive.Text = editedInfo.MinimumThriveVersion;
        maximumThrive.Text = editedInfo.MaximumThriveVersion;
        pckName.Text = editedInfo.PckToLoad;
        modAssembly.Text = editedInfo.ModAssembly;
        assemblyModClass.Text = editedInfo.AssemblyModClass;
        assemblyModAutoHarmony.ButtonPressed = editedInfo.UseAutoHarmony ?? false;
    }

    private bool ReadControlsToEditedInfo()
    {
        editedInfo!.Name = name.Text;
        editedInfo.InternalName = internalName.Text;
        editedInfo.Author = author.Text;
        editedInfo.Version = version.Text;
        editedInfo.Description = description.Text;
        editedInfo.LongDescription = longDescription.Text;
        editedInfo.Icon = iconFile.Text;
        editedInfo.License = license.Text;
        editedInfo.RecommendedThriveVersion = recommendedThrive.Text;
        editedInfo.MinimumThriveVersion = minimumThrive.Text;
        editedInfo.MaximumThriveVersion = maximumThrive.Text;
        editedInfo.PckToLoad = pckName.Text;
        editedInfo.ModAssembly = modAssembly.Text;
        editedInfo.AssemblyModClass = assemblyModClass.Text;
        editedInfo.UseAutoHarmony = assemblyModAutoHarmony.ButtonPressed;

        if (string.IsNullOrWhiteSpace(infoUrl.Text))
        {
            editedInfo.InfoUrl = null;
        }
        else
        {
            if (Uri.TryCreate(infoUrl.Text, UriKind.Absolute, out var parsed))
            {
                editedInfo.InfoUrl = parsed;
            }
            else
            {
                SetError(Localization.Translate("INVALID_URL_FORMAT"));
                return false;
            }
        }

        return true;
    }

    private string? ValidateFormData()
    {
        if (editedInfo == null)
            throw new InvalidOperationException("Validate form called without editing info");

        if (string.IsNullOrWhiteSpace(editedInfo.InternalName))
        {
            SetError(Localization.Translate("INTERNAL_NAME_REQUIRED"));
            return null;
        }

        if (!char.IsUpper(editedInfo.InternalName, 0))
        {
            SetError(Localization.Translate("INTERNAL_NAME_REQUIRES_CAPITAL"));
            return null;
        }

        if (ModLoader.LoadModInfo(editedInfo.InternalName, false) != null)
        {
            SetError(Localization.Translate("INTERNAL_NAME_IN_USE"));
            return null;
        }

        var serialized = new StringWriter();

        var finalResult = new FullModDetails(editedInfo.InternalName,
            Path.Combine(Constants.ModLocations[Constants.ModLocations.Count - 1], internalName.Text), editedInfo);

        try
        {
            JsonSerializer.Create().Serialize(serialized, finalResult);
        }
        catch (JsonSerializationException e)
        {
            SetError(Localization.Translate("MISSING_OR_INVALID_REQUIRED_FIELD").FormatSafe(e.Message));
            return null;
        }

        try
        {
            ModManager.ValidateModInfo(editedInfo, true);
        }
        catch (Exception e)
        {
            SetError(Localization.Translate("ADDITIONAL_VALIDATION_FAILED").FormatSafe(e.Message));
            return null;
        }

        return serialized.ToString();
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
