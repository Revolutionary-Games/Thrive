using System;

/// <summary>
///   Displays a popup that tells the player that a special cell type is unset
/// </summary>
public partial class SpecialCellTypeUnsetPopup : CustomConfirmationDialog
{
    public void DisplayForCellType(SpecialCellArchetype specialCellArchetype)
    {
        switch (specialCellArchetype)
        {
            case SpecialCellArchetype.Spore:
                WindowTitle = Localization.Translate("NO_SPORE_CELL_TYPE_SET_TITLE");
                DialogText = Localization.Translate("NO_SPORE_CELL_TYPE_SET");
                break;
            case SpecialCellArchetype.GameteA:
            case SpecialCellArchetype.GameteB:
                WindowTitle = Localization.Translate("NO_GAMETE_CELL_TYPE_SET_TITLE");
                DialogText = Localization.Translate("NO_GAMETE_CELL_TYPE_SET");
                break;
            default:
                throw new NotImplementedException($"Unimplemented special cell type: {specialCellArchetype}");
        }

        PopupCenteredShrink();
    }
}
