using Godot;

/// <summary>
///   Handles letting the player know that the game is experiencing unhandled code errors
/// </summary>
public partial class UnHandledErrorsGUI : Control
{
    [Export]
    public float ErrorDisplayTime = 30;

#pragma warning disable CA2213
    private static UnHandledErrorsGUI? instance;

    [Export]
    private Label errorCount = null!;

    [Export]
    private ErrorDialog errorPopup = null!;

    [Export]
    private CheckBox ignoreFurtherErrors = null!;

    [Export]
    private Control popupModsUsedWarning = null!;

    [Export]
    private Control counterModsUsedWarning = null!;
#pragma warning restore CA2213

    private double timeSinceLastError;
    private int errorCountValue;
    private bool showingErrorNumber;

    private bool suppressErrorPopups;

    public static UnHandledErrorsGUI? Instance => instance;

    public override void _Ready()
    {
        if (instance != null)
            GD.PrintErr("Multiple unhandled errors GUIs");

        instance = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (instance == this)
            instance = null;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!showingErrorNumber)
            return;

        timeSinceLastError += delta;
        if (timeSinceLastError > ErrorDisplayTime)
        {
            showingErrorNumber = false;
            errorCount.Visible = false;
            counterModsUsedWarning.Visible = false;
            timeSinceLastError = 0;
            errorCountValue = 0;
        }
    }

    public void ReportError(string error, string additionalContext = "")
    {
        Visible = true;

        ++errorCountValue;
        timeSinceLastError = 0;

        showingErrorNumber = true;
        errorCount.Visible = true;
        errorCount.Text = Localization.Translate("UNHANDLED_ERROR_COUNT").FormatSafe(errorCountValue);

        var usesMods = ModLoader.Instance.HasEnabledMods();
        counterModsUsedWarning.Visible = usesMods;

        if (!suppressErrorPopups)
        {
            if (!errorPopup.Visible)
            {
                if (usesMods)
                    additionalContext = "(MODS ENABLED)\n" + additionalContext;

                errorPopup.ExceptionInfo = error;

                errorPopup.ExceptionExtraContext = additionalContext;

                popupModsUsedWarning.Visible = usesMods;

                errorPopup.PopupCenteredShrink();
            }
        }
    }

    private void OnErrorPopupClose()
    {
        if (ignoreFurtherErrors.ButtonPressed)
        {
            GD.Print("Suppressing further error popups");
            suppressErrorPopups = true;
        }
    }
}
