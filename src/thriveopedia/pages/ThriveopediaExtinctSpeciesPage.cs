using Godot;

/// <summary>
///   A page just saying the species is extinct
/// </summary>
public partial class ThriveopediaExtinctSpeciesPage : ThriveopediaPage, IThriveopediaPage, ITransientPage
{
    // TODO: this should probably allow unpinning if the species was pinned before

#pragma warning disable CA2213
    [Export]
    private CustomRichTextLabel mainText = null!;

#pragma warning restore CA2213

    public string PageName { get; private set; } = "ERROR_UNSET_EXTINCT_SPECIES_ID";

    public string TranslatedPageName =>
        Localization.Translate("THRIVEOPEDIA_PAGE_EXTINCT_SPECIES").FormatSafe(SpeciesId);

    public string ParentPageName => "WorldSpecies";

    public uint SpeciesId
    {
        get;
        set
        {
            field = value;
            PageName = $"species:{SpeciesId}";
        }
    }

    public string? TranslatedPageBody => null;
    public string? TranslatedAdicionalSearchContent => null;

    public bool Pinned => false;

    public override void _Ready()
    {
        base._Ready();

        mainText.ExtendedBbcode = Localization.Translate("THRIVEOPEDIA_EXTINCT_SPECIES_TEXT").FormatSafe(SpeciesId);
    }
}
