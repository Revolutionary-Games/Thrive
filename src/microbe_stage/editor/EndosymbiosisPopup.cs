using System;

/// <summary>
///   GUI for the player to manage endosymbiosis process
/// </summary>
public partial class EndosymbiosisPopup : CustomWindow
{
    private EndosymbiosisData? endosymbiosisData;

    public void UpdateData(EndosymbiosisData endosymbiosis)
    {
        endosymbiosisData = endosymbiosis;
        UpdateGUIState();
    }

    private void UpdateGUIState()
    {
        if (endosymbiosisData == null)
            throw new InvalidOperationException("No data set");
    }
}
