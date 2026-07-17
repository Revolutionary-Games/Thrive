using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Godot;

/// <summary>
///   Thriveopedia page for displaying information about a specific species.
/// </summary>
public partial class ThriveopediaSpeciesInfoPage : ThriveopediaPage, IThriveopediaPage, ITransientPage
{
    /// <summary>
    ///   This is a shared resource used in the construction of the TranslatedAdditionalSearchContent
    ///   This is being done to reduce the amount of HashSets being made when requesting
    /// </summary>
    private static readonly HashSet<string> HashSetPool = new();

#pragma warning disable CA2213
    [Export]
    private CustomRichTextLabel mainText = null!;

    [Export]
    private SpeciesPreview mainPreview = null!;

    [Export]
    private CellHexesPreview hexesPreview = null!;

    [Export]
    private Button pinButton = null!;

    [Export]
    private Label stageInfo = null!;

    [Export]
    private Label sizeInfo = null!;

    [Export]
    private Label internalName = null!;

#pragma warning restore CA2213

    private bool changingAutomatically;

    private string cachedName = "ERROR_UNINITIALIZED_SPECIES_INFO";

    private string? cacheTranslatedAdditionalSearchContent;

    public string PageName => cachedName;

    public string TranslatedPageName =>
        Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE").FormatSafe(SpeciesToShow.FormattedName);

    public string? TranslatedPageBody => null;

    public string TranslatedAdditionalSearchContent
    {
        get
        {
            if (cacheTranslatedAdditionalSearchContent != null)
            {
                return cacheTranslatedAdditionalSearchContent;
            }

            StringBuilder builder = new StringBuilder();

            if (SpeciesToShow is MulticellularSpecies multicellularSpecies)
            {
                builder.AppendLine(Localization.Translate("MULTICELLULAR"));

                foreach (CellTemplate cell in multicellularSpecies.ModifiableGameplayCells)
                {
                    foreach (IReadOnlyOrganelleTemplate organelle in cell.Organelles)
                    {
                        HashSetPool.Add(organelle.Definition.ReadableName);
                    }
                }
            }

            if (SpeciesToShow is MicrobeSpecies microbeSpecies)
            {
                builder.AppendLine(Localization.Translate("MICROBE"));

                foreach (OrganelleTemplate organelle in microbeSpecies.Organelles)
                {
                    HashSetPool.Add(organelle.Definition.ReadableName);
                }
            }

            foreach (var item in HashSetPool)
            {
                builder.AppendLine(item);
            }

            builder.AppendLine(Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE_SEARCHTAGS"));

            cacheTranslatedAdditionalSearchContent = builder.ToString();

            HashSetPool.Clear();

            return cacheTranslatedAdditionalSearchContent;
        }
    }

    public string ParentPageName => "WorldSpecies";

    /// <summary>
    ///   Has to be set to the species to show on this page before doing anything else with this object
    /// </summary>
    public required Species SpeciesToShow
    {
        get;
        set
        {
            field = value;
            cachedName = $"species:{SpeciesToShow.ID}";
        }
    }

    /// <summary>
    ///   Only pinned pages persist, all others are deleted to not clutter the tree
    /// </summary>
    public bool Pinned { get; set; }

    public override void _Ready()
    {
        base._Ready();
        RebuildInfo();
        RefreshPinStatus();
    }

    public override void OnThriveopediaOpened()
    {
        RebuildInfo();
    }

    public override void OnTranslationsChanged()
    {
        RebuildInfo();
    }

    public override void UpdateCurrentWorldDetails()
    {
        RebuildInfo();

        RefreshPinStatus();
    }

    private void RefreshPinStatus()
    {
        if (CurrentGame == null)
            return;

        bool pinned = CurrentGame.ThriveopediaData.IsPagePinned(this);

        changingAutomatically = true;
        Pinned = pinned;
        pinButton.ButtonPressed = pinned;
        changingAutomatically = false;
    }

    private void RebuildInfo()
    {
        var areas = -1;

        if (CurrentGame != null)
        {
            areas = 0;

            foreach (var (_, patch) in CurrentGame.GameWorld.Map.Patches)
            {
                if (patch.FindSpeciesByID(SpeciesToShow.ID) != null)
                {
                    ++areas;
                }
            }
        }

        var stage = SpeciesToShow.StageForDisplay;

        var stageText = Localization.Translate(stage.GetAttribute<DescriptionAttribute>().Description);

        var population = Species.ScalePopulationByType(SpeciesToShow, SpeciesToShow.Population);

        mainText.ExtendedBbcode = Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE_INTRO_TEXT")
            .FormatSafe(SpeciesToShow.FormattedNameBbCode, population.FormatNumber(), areas, stageText);

        cacheTranslatedAdditionalSearchContent = null;

        UpdateInfoBox();
    }

    private void UpdateInfoBox()
    {
        stageInfo.Text = SpeciesToShow.StageForDisplay.GetAttribute<DescriptionAttribute>().Description;
        internalName.Text = SpeciesToShow.FormattedIdentifier;

        // TODO: if the page is kept open and the player edits the species and goes back to the editor, the size
        // doesn't get updated for some reason (but the graphical visualization does get updated). Even though opening
        // the Thriveopedia should trigger a call to this method again.
        float size = 0;

        // Set species preview
        mainPreview.PreviewSpecies = SpeciesToShow;

        if (SpeciesToShow is MicrobeSpecies microbeSpecies)
        {
            size = microbeSpecies.BaseHexSize;
            hexesPreview.PreviewSpecies = microbeSpecies;
        }
        else if (SpeciesToShow is MulticellularSpecies multicellularSpecies)
        {
            // TODO: add up hexes of all cells?
            size = multicellularSpecies.GameplayCells.Count;
            hexesPreview.PreviewSpecies = multicellularSpecies;
        }
        else if (SpeciesToShow is MacroscopicSpecies macroscopicSpecies)
        {
            hexesPreview.Visible = false;

            foreach (var metaball in macroscopicSpecies.BodyLayout)
            {
                var sphereVolume = metaball.Radius * metaball.Radius * Math.PI;
                size += (float)sphereVolume;
            }

            size = (float)Math.Round(size, 2);
        }

        // TODO: show nicely with like a "2 hexes" etc. suffix
        sizeInfo.Text = size.ToString(CultureInfo.CurrentCulture);
    }

    private void OnPinPressed(bool pressed)
    {
        if (changingAutomatically || CurrentGame == null)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        Pinned = pressed;
        CurrentGame.ThriveopediaData.SetPagePinned(this, Pinned);
    }

    private void DummyTranslations()
    {
        // This translation is kept around in case we want a pinned checkbox back in the future
        Localization.Translate("PAGE_PINNED");
    }
}
