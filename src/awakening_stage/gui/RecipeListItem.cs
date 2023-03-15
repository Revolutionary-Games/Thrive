using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public class RecipeListItem : Button
{
    private readonly StringBuilder topLevelStringBuilder = new();
    private readonly StringBuilder materialsStringBuilder = new();

#pragma warning disable CA2213
    private CustomRichTextLabel? textLabel;
#pragma warning restore CA2213

    private CraftingRecipe? displayedRecipe;
    private IReadOnlyDictionary<WorldResource, int> availableMaterials = new Dictionary<WorldResource, int>();

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
            textLabel.RectSize = RectSize - new Vector2(2, 2);
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

        bool canCraft = displayedRecipe.CanCraft(availableMaterials);

        if (!canCraft)
        {
            // Apply a different colour to show uncraftable status
            topLevelStringBuilder.Append("[color=]");
        }

        // Setup the materials list display
        bool first = true;

        foreach (var tuple in displayedRecipe.RequiredResources)
        {
            if (!first)
            {
                materialsStringBuilder.Append(", ");
            }

            if (!displayedRecipe.HasEnoughResource(tuple.Key, tuple.Value))
            {
                // Show not enough icon
                throw new NotImplementedException();
            }
            else
            {
                // Has enough
                throw new NotImplementedException();
            }

            materialsStringBuilder.Append(tuple.Value);

            // Icon for this material
            throw new NotImplementedException();

            first = false;
        }

        TranslationServer.Translate("CRAFTING_RECIPE_DISPLAY").FormatSafe(displayedRecipe.Name, materialsStringBuilder);

        if (!canCraft)
        {
            // Reset the top level colour
            topLevelStringBuilder.Append("[/color]");
        }
    }
}
