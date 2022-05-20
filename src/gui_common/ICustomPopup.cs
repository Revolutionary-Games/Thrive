using Godot;

/// <summary>
///   Interface for any custom popups inheriting Popup.
/// </summary>
public interface ICustomPopup
{
    /// <summary>
    ///   If true and <see cref="Popup.PopupExclusive"/> is true, pressing ESC key will close
    ///   the popup.
    /// </summary>
    public bool ExclusiveAllowCloseOnEscape { get; set; }

    /// <summary>
    ///   Custom Show call for customizable Show behavior.
    /// </summary>
    public void CustomShow();

    /// <summary>
    ///   Custom Hide call for customizable Hide behavior.
    /// </summary>
    public void CustomHide();
}
