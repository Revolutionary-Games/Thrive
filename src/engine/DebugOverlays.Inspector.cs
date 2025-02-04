using Godot;

/// <summary>
///   Inspector part of the debug overlay, displays debug coordinates, etc.
///   Debug equivalent of <see cref="PlayerInspectInfo"/>.
/// </summary>
public partial class DebugOverlays
{
    private Vector3 positionCoordinates;
    private Vector3 lookingAtCoordinates;

    private float heat;
    private bool hasHeat;

    public void ReportPositionCoordinates(Vector3 coordinates)
    {
        positionCoordinates = coordinates;
    }

    public void ReportLookingAtCoordinates(Vector3 coordinates)
    {
        lookingAtCoordinates = coordinates;
    }

    public void ReportHeatValue(float heatValue)
    {
        heat = heatValue;

        // Enable heat display with a valid value
        hasHeat = float.IsNormal(heat);
    }

    public void StopHeatReporting()
    {
        ReportHeatValue(float.NaN);
    }

    private void UpdateInspector()
    {
        var coordinates = Localization.Translate("DEBUG_COORDINATES")
            .FormatSafe(positionCoordinates, lookingAtCoordinates);

        if (hasHeat)
        {
            debugCoordinates.Text = coordinates + "\n" + Localization.Translate("DEBUG_HEAT_AT_CURSOR")
                .FormatSafe(heat);
        }
        else
        {
            debugCoordinates.Text = coordinates;
        }
    }

    private void OnInspectorToggled()
    {
        inspectorCheckbox.ButtonPressed = !inspectorCheckbox.ButtonPressed;
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
