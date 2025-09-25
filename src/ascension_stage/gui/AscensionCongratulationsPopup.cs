/// <summary>
///   Confirms the player wants to descend and allows picking the descension perk
/// </summary>
public partial class AscensionCongratulationsPopup : CustomConfirmationDialog
{
    public void ShowWithInfo(GameProperties currentGame)
    {
        // TODO: add a total playthrough length
        DialogText = Localization.Translate("ASCENSION_CONGRATULATIONS_CONTENT")
            .FormatSafe(currentGame.AscensionCounter);

        if (currentGame.CheatsUsed)
        {
            DialogText += "\n\n" + Localization.Translate("CHEATS_USED_NOTICE");
        }

        PopupCenteredShrink();
    }
}
