using System.Linq;
using Godot;

/// <summary>
///   Displays the amount of stored resource in the strategy stages
/// </summary>
public partial class ResourceDisplayBar : HBoxContainer
{
    [Export]
    public NodePath? EarlyResourcesContainerPath;

    [Export]
    public NodePath ScienceIndicatorContainerPath = null!;

    [Export]
    public NodePath ScienceAmountLabelPath = null!;

    [Export]
    public NodePath LateResourcesContainerPath = null!;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color NormalResourceAmountColour = new(1, 1, 1);

    [Export(PropertyHint.ColorNoAlpha)]
    public Color FullResourceAmountColour = new(1, 1, 0);

    [Export(PropertyHint.ColorNoAlpha)]
    public Color CriticalResourceAmountColour = new(1, 0, 0);

#pragma warning disable CA2213
    [Export]
    public LabelSettings AmountLabelFont = null!;

    private Container earlyResourcesContainer = null!;
    private Container lateResourcesContainer = null!;

    private Control scienceIndicatorContainer = null!;
    private Label scienceAmountLabel = null!;
#pragma warning restore CA2213

    private ChildObjectCache<WorldResource, DisplayAmount> resourceDisplayCache = null!;

    public override void _Ready()
    {
        earlyResourcesContainer = GetNode<Container>(EarlyResourcesContainerPath);
        lateResourcesContainer = GetNode<Container>(LateResourcesContainerPath);

        scienceIndicatorContainer = GetNode<Control>(ScienceIndicatorContainerPath);
        scienceAmountLabel = GetNode<Label>(ScienceAmountLabelPath);

        scienceIndicatorContainer.Visible = false;
        scienceAmountLabel.LabelSettings = AmountLabelFont;

        // TODO: remove once this is used
        lateResourcesContainer.Visible = false;

        resourceDisplayCache =
            new ChildObjectCache<WorldResource, DisplayAmount>(earlyResourcesContainer, CreateDisplayAmount);
    }

    public void UpdateResources(SocietyResourceStorage resourceStorage)
    {
        // TODO: some resources should not always be shown in the bar
        // TODO: and some resources should be in the late resources container

        resourceDisplayCache.UnMarkAll();

        // The resources are sorted here to ensure consistent order of icons in the GUI, this would not really be
        // necessary each frame so TODO something better to only sort when required (for example when new items are
        // created)
        foreach (var pair in resourceStorage.GetAllResources().OrderBy(t => t.Key.InternalName))
        {
            var display = resourceDisplayCache.GetChild(pair.Key);

            display.SetAmount(pair.Value, pair.Value >= resourceStorage.Capacity);
        }

        resourceDisplayCache.DeleteUnmarked();
    }

    public void UpdateScienceAmount(float amount)
    {
        scienceIndicatorContainer.Visible = true;

        if (amount <= 0)
        {
            scienceAmountLabel.AddThemeColorOverride("font_color", CriticalResourceAmountColour);
            scienceAmountLabel.Text = "0";
            return;
        }

        scienceAmountLabel.AddThemeColorOverride("font_color", NormalResourceAmountColour);
        scienceAmountLabel.Text =
            StringUtils.FormatPositiveWithLeadingPlus(StringUtils.ThreeDigitFormat(amount), amount);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (EarlyResourcesContainerPath != null)
            {
                EarlyResourcesContainerPath.Dispose();
                ScienceIndicatorContainerPath.Dispose();
                ScienceAmountLabelPath.Dispose();
                LateResourcesContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private DisplayAmount CreateDisplayAmount(WorldResource resource)
    {
        return new DisplayAmount(resource, FullResourceAmountColour, NormalResourceAmountColour, AmountLabelFont);
    }

    private partial class DisplayAmount : HBoxContainer
    {
        private readonly Color maxColour;
        private readonly Color normalColour;

#pragma warning disable CA2213
        private readonly Label amountLabel;
#pragma warning restore CA2213

        private string? previousAmount;
        private bool previousMax;

        public DisplayAmount(WorldResource resource, Color maxColour, Color normalColour, LabelSettings font)
        {
            this.maxColour = maxColour;
            this.normalColour = normalColour;

            amountLabel = new Label
            {
                Text = "0",
                VerticalAlignment = VerticalAlignment.Center,
            };

            amountLabel.LabelSettings = font;

            // TODO: reserving space for the characters would help to have the display jitter less

            AddChild(amountLabel);

            AddChild(new TextureRect
            {
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(16, 16),
                Texture = resource.Icon,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            });

            // TODO: tooltips showing the resource name and where it comes from and what consumes it

            // TODO: click callbacks
        }

        public void SetAmount(float amount, bool max)
        {
            var newAmountString = StringUtils.ThreeDigitFormat(amount);

            // A bit of an early skip to skip some operations if nothing has changed
            if (previousAmount == newAmountString && max == previousMax)
                return;

            previousAmount = newAmountString;
            previousMax = max;

            amountLabel.Text = newAmountString;

            amountLabel.AddThemeColorOverride("font_color", max ? maxColour : normalColour);
        }
    }
}
