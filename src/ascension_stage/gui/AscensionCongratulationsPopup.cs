/// <summary>
///   Confirms the player wants to descend and allows picking the descension perk
/// </summary>
public partial class AscensionCongratulationsPopup : CustomConfirmationDialog
{
    public void ShowWithInfo(GameProperties currentGame)
    {
        DialogText = Localization.Translate("ASCENSION_CONGRATULATIONS_CONTENT")
            .FormatSafe(currentGame.AscensionCounter);

        PopupCenteredShrink();
    }
}
