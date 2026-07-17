using Godot;

/// <summary>
///   Parent page for all species info pages in the current world
/// </summary>
public partial class ThriveopediaWorldSpeciesPage : ThriveopediaPage, IThriveopediaPage
{
#pragma warning disable CA2213
    [Export]
    private Container subPagesList = null!;

    [Export]
    private Control noPagesHeading = null!;

#pragma warning restore CA2213

    private ThriveopediaGameData? registeredListener;

    private bool dirty;

    public string PageName => "WorldSpecies";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_WORLD_SPECIES_TITLE");
    public string? TranslatedPageBody => null;
    public string TranslatedAdditionalSearchContent => Localization.Translate("THRIVEOPEDIA_WORLD_SPECIES_SEARCHTAGS");
    public string ParentPageName => "CurrentWorld";

    public override void _Ready()
    {
        base._Ready();
        RefreshList();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (registeredListener != null)
        {
            registeredListener.PinnedPageChanged -= OnPinStatusChanged;
            registeredListener = null;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
            if (dirty)
                RefreshList();
        }
    }

    public void OnPinStatusChanged()
    {
        dirty = true;
    }

    public override void OnThriveopediaOpened()
    {
        dirty = true;
    }

    public override void OnTranslationsChanged()
    {
        if (Visible)
        {
            RefreshList();
        }
        else
        {
            dirty = true;
        }
    }

    public override void UpdateCurrentWorldDetails()
    {
        RefreshList();

        if (registeredListener != null)
        {
            registeredListener.PinnedPageChanged -= OnPinStatusChanged;
            registeredListener = null;
        }

        registeredListener = CurrentGame?.ThriveopediaData;
        registeredListener?.PinnedPageChanged += OnPinStatusChanged;
    }

    private void RefreshList()
    {
        if (CurrentGame == null)
            return;

        subPagesList.QueueFreeChildren();

        bool hadItems = false;

        // TODO: sort?
        foreach (var pinnedSpecies in CurrentGame.ThriveopediaData.CalculatePinnedSpecies())
        {
            var species = ThriveopediaManager.GetActiveSpeciesData(pinnedSpecies);

            var button = new LinkButton();

            if (species == null)
            {
                button.Text = Localization.Translate("THRIVEOPEDIA_PAGE_EXTINCT_SPECIES").FormatSafe(pinnedSpecies);
            }
            else
            {
                button.Text = species.FormattedName;
            }

            button.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => ThriveopediaManager.OpenPage("species:" + pinnedSpecies)));

            subPagesList.AddChild(button);
            hadItems = true;
        }

        noPagesHeading.Visible = !hadItems;
        dirty = false;
    }

    private void OnPinStatusChanged(string page, bool pinned)
    {
        OnPinStatusChanged();
    }
}
