using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Item in the crafting recipe list that can be selected to be crafted
/// </summary>
public partial class RecipeListItem : Button
{
    [Export(PropertyHint.ColorNoAlpha)]
    public Color UncraftableItemColor = new(0.5f, 0.5f, 0.5f);

    [Export]
    public int MarginAroundLabel = 6;

    private readonly StringBuilder topLevelStringBuilder = new();
    private readonly StringBuilder materialsStringBuilder = new();

#pragma warning disable CA2213
    private CustomRichTextLabel? textLabel;
#pragma warning restore CA2213

    private CraftingRecipe? displayedRecipe;
    private IReadOnlyDictionary<WorldResource, int> availableMaterials = new Dictionary<WorldResource, int>();

    [Signal]
    public delegate void OnSelectedEventHandler(RecipeListItem selectedItem);

    public CraftingRecipe? DisplayedRecipe
    {
        get => displayedRecipe;
        set
        {
            if (displayedRecipe == value)
                return;

            displayedRecipe = value;
            UpdateShownRecipe();
        }
    }

    public IReadOnlyDictionary<WorldResource, int> AvailableMaterials
    {
        get => availableMaterials;
        set
        {
            availableMaterials = value;
            UpdateShownRecipe();
        }
    }

    public override void _Ready()
    {
        textLabel = GetNode<CustomRichTextLabel>("Label");

        // TODO: tooltip that on hover shows the full names of the required resources and could also show a description
        // of the recipe / associated technology

        SetLabelSize();
        UpdateShownRecipe();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationResized)
        {
            SetLabelSize();
        }
        else if (what == NotificationTranslationChanged)
        {
            UpdateShownRecipe();
        }
    }

    private void SetLabelSize()
    {
        if (textLabel != null)
        {
            textLabel.Size = Size - new Vector2(MarginAroundLabel, MarginAroundLabel);
            var halfMargin = MarginAroundLabel / 2;
            textLabel.Position = new Vector2(halfMargin, halfMargin);
        }
    }

    private void UpdateShownRecipe()
    {
        if (textLabel == null)
            return;

        if (displayedRecipe == null)
        {
            textLabel.ExtendedBbcode = null;
            return;
        }

        materialsStringBuilder.Clear();
        topLevelStringBuilder.Clear();

        bool canCraft = displayedRecipe.CanCraft(availableMaterials) == null;

        if (!canCraft)
        {
            // Apply a different colour to show uncraftable status
            topLevelStringBuilder.Append($"[color=#{UncraftableItemColor.ToHtml(false)}]");
        }

        // Setup the materials list display
        ResourceAmountHelpers.CreateRichTextForResourceAmounts(displayedRecipe.RequiredResources, availableMaterials,
            materialsStringBuilder);

        topLevelStringBuilder.Append(Localization.Translate("CRAFTING_RECIPE_DISPLAY")
            .FormatSafe(displayedRecipe.Name, materialsStringBuilder));

        // TODO: display for recipes that require tools to be present but won't consume them

        if (!canCraft)
        {
            // Reset the top level colour
            topLevelStringBuilder.Append("[/color]");
        }

        textLabel.ExtendedBbcode = topLevelStringBuilder.ToString();
    }

    private void OnToggledChanged(bool pressed)
    {
        if (pressed)
            EmitSignal(SignalName.OnSelected, this);
    }
}
