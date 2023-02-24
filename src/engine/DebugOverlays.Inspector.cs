using Godot;

public partial class DebugOverlays
{
    private Vector3 positionCoords;
    private Vector3 lookingAtCoords;

    public void ReportPositionCoords(Vector3 coords)
    {
        positionCoords = coords;
    }

    public void ReportLookingAtCoords(Vector3 coords)
    {
        lookingAtCoords = coords;
    }

    private void UpdateInspector()
    {
        debugCoordinates.Text = TranslationServer.Translate("DEBUG_COORDINATES").FormatSafe(
            positionCoords, lookingAtCoords);
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
