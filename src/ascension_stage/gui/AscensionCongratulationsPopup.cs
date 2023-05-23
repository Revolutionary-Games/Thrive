using Godot;

/// <summary>
///   Confirms the player wants to descend and allows picking the descension perk
/// </summary>
public class AscensionCongratulationsPopup : CustomConfirmationDialog
{
    public void ShowWithInfo(GameProperties currentGame)
    {
        DialogText = TranslationServer.Translate("ASCENSION_CONGRATULATIONS_CONTENT")
            .FormatSafe(currentGame.AscensionCounter);

        PopupCenteredShrink();
    }
}
