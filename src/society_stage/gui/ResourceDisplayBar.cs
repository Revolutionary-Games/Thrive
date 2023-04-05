using Godot;

public class ResourceDisplayBar : HBoxContainer
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

        foreach (var pair in resourceStorage.GetAllResources())
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
            scienceAmountLabel.AddColorOverride("font_color", CriticalResourceAmountColour);
            scienceAmountLabel.Text = "0";
            return;
        }

        scienceAmountLabel.AddColorOverride("font_color", NormalResourceAmountColour);
        scienceAmountLabel.Text = "+" + StringUtils.ThreeDigitFormat(amount);
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
        return new DisplayAmount(resource, FullResourceAmountColour, NormalResourceAmountColour);
    }

    private class DisplayAmount : HBoxContainer
    {
        private readonly Color maxColour;
        private readonly Color normalColour;

#pragma warning disable CA2213
        private readonly Label amountLabel;
#pragma warning restore CA2213

        private string? previousAmount;
        private bool previousMax;

        public DisplayAmount(WorldResource resource, Color maxColour, Color normalColour)
        {
            this.maxColour = maxColour;
            this.normalColour = normalColour;

            amountLabel = new Label
            {
                Text = "0",
            };

            AddChild(amountLabel);

            AddChild(new TextureRect
            {
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                RectMinSize = new Vector2(24, 24),
                Texture = resource.Icon,
                Expand = true,
            });

            // TODO: tooltips showing the resource name and where it comes from and what consumes it

            // TODO: click callbacks
        }

        public void SetAmount(float amount, bool max)
        {
            var newAmountString = amount > 0 ?
                "+" + StringUtils.ThreeDigitFormat(amount) :
                StringUtils.ThreeDigitFormat(amount);

            // A bit of an early skip to skip some operations if nothing has changed
            if (previousAmount == newAmountString && max == previousMax)
                return;

            previousAmount = newAmountString;
            previousMax = max;

            amountLabel.Text = newAmountString;

            amountLabel.AddColorOverride("font_color", max ? maxColour : normalColour);
        }
    }
}
