using Godot;

/// <summary>
///   Inspector part of the debug overlay, displays debug coordinates, etc.
///   Debug equivalent of <see cref="PlayerInspectInfo"/>.
/// </summary>
public partial class DebugOverlays
{
    private Vector3 positionCoordinates;
    private Vector3 lookingAtCoordinates;

    public void ReportPositionCoordinates(Vector3 coordinates)
    {
        positionCoordinates = coordinates;
    }

    public void ReportLookingAtCoordinates(Vector3 coordinates)
    {
        lookingAtCoordinates = coordinates;
    }

    private void UpdateInspector()
    {
        debugCoordinates.Text = TranslationServer.Translate("DEBUG_COORDINATES")
            .FormatSafe(positionCoordinates, lookingAtCoordinates);
    }

    private void OnInspectorToggled()
    {
        inspectorCheckbox.Pressed = !inspectorCheckbox.Pressed;
    }

    private void OnInspectorCheckBoxToggled(bool state)
    {
        if (inspectorDialog.Visible == state)
            return;

        if (state)
        {
            inspectorDialog.Show();
        }
        else
        {
            inspectorDialog.Hide();
        }
    }
}
