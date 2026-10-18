/// <summary>
///   Displays an error when a cell type is not used in the body plan
/// </summary>
public partial class UnusedCellTypePopup : CustomConfirmationDialog
{
    private string? unusedCellTypeName;

    public override void _EnterTree()
    {
        base._EnterTree();

        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    public void DisplayForCellType(string cellTypeName)
    {
        unusedCellTypeName = cellTypeName;
        UpdateDialogText();
        PopupCenteredShrink();
    }

    private void OnTranslationsChanged()
    {
        if (unusedCellTypeName != null)
            UpdateDialogText();
    }

    private void UpdateDialogText()
    {
        DialogText = Localization.Translate("UNUSED_CELL_TYPE_IN_BODY_PLAN_ERROR")
            .FormatSafe(unusedCellTypeName);
    }
}
