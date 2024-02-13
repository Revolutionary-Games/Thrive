using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   The panel that shows what the player is hovering over/inspecting.
/// </summary>
public class MouseHoverPanel : PanelContainer
{
    [Export]
    public NodePath? CategoriesContainerPath;

    [Export]
    public NodePath NothingHereContainerPath = null!;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private Container categoriesContainer = null!;
    private Container nothingHereContainer = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private Dictionary<string, MouseHoverCategory> categories = new();

    /// <summary>
    ///   The array of category controls ordered based on their position in the scene tree.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: this being a Godot.Array causes the enumeration of this to allocate memory each time
    ///   </para>
    /// </remarks>
    private Array categoryControls = new();

    public override void _Ready()
    {
        categoriesContainer = GetNode<Container>(CategoriesContainerPath);
        nothingHereContainer = GetNode<Container>(NothingHereContainerPath);
    }

    public override void _Process(float delta)
    {
        var visibleEntriesCount = 0;

        MouseHoverCategory? firstVisibleCategory = null;

        foreach (MouseHoverCategory category in categoryControls)
        {
            var entriesCount = category.TotalEntriesCount;
            category.Visible = entriesCount > 0;
            category.SeparatorVisible = firstVisibleCategory != null;
            visibleEntriesCount += entriesCount;

            if (firstVisibleCategory == null && category.Visible)
                firstVisibleCategory = category;
        }

        nothingHereContainer.Visible = visibleEntriesCount <= 0;
    }

    public void AddCategory(string internalName, LocalizedString displayName)
    {
        if (categories.ContainsKey(internalName))
            throw new InvalidOperationException("Category already exist for \"" + internalName + "\"");

        var categoryControl = new MouseHoverCategory(displayName);
        categories.Add(internalName, categoryControl);
        categoriesContainer.AddChild(categoryControl);

        categoryControls = categoriesContainer.GetChildren();
    }

    public void MoveCategory(string internalName, int position)
    {
        if (!categories.TryGetValue(internalName, out MouseHoverCategory categoryControl))
            throw new InvalidOperationException("Category doesn't exist for \"" + internalName + "\"");

        categoriesContainer.MoveChild(categoryControl, position);

        categoryControls = categoriesContainer.GetChildren();
    }

    /// <summary>
    ///   Adds a new inspected entity entry to the the given category. Throws
    ///   <see cref="System.InvalidOperationException"/> if the category doesn't exist.
    /// </summary>
    /// <param name="category">The category the inspectable falls under.</param>
    /// <param name="text">The inspectable's display name.</param>
    /// <param name="icon">The icon representing the inspectable.</param>
    /// <returns>The control created for the inspectable.</returns>
    /// <exception cref="System.InvalidOperationException">If the given category doesn't exist.</exception>
    public InspectedEntityLabel AddItem(string category, string text, Texture? icon = null)
    {
        if (!categories.TryGetValue(category, out MouseHoverCategory categoryControl))
            throw new InvalidOperationException("Can't add item, category doesn't exist for \"" + category + "\"");

        var label = new InspectedEntityLabel(text, icon);
        categoryControl.EmplaceLabel(label);

        return label;
    }

    /// <summary>
    ///   Clears all entries of the given category.
    /// </summary>
    /// <param name="category">If null, all categories.</param>
    public void ClearEntries(string? category = null)
    {
        if (category == null)
        {
            foreach (var categoryControl in categories)
            {
                categoryControl.Value.ClearEntries();
            }
        }
        else if (categories.TryGetValue(category, out MouseHoverCategory categoryControl))
        {
            categoryControl.ClearEntries();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CategoriesContainerPath != null)
            {
                CategoriesContainerPath.Dispose();
                NothingHereContainerPath.Dispose();
                categoryControls.Dispose();
            }
        }
    }

    public class MouseHoverCategory : VBoxContainer
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private Label titleLabel;
        private VBoxContainer container;
        private HSeparator separator;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        ///   Cached number of added entity labels. This needs to be used to avoid unnecessary memory allocations each
        ///   frame.
        /// </summary>
        private int totalEntityLabels;

        private LocalizedString title;

        public MouseHoverCategory(LocalizedString title)
        {
            this.title = title;

            titleLabel = new Label
            {
                Text = title.ToString(),
                RectMinSize = new Vector2(0, 20),
            };

            container = new VBoxContainer();
            separator = new HSeparator { RectMinSize = new Vector2(0, 5) };
        }

        public int TotalEntriesCount
        {
            get
            {
                return totalEntityLabels;
            }
        }

        public bool SeparatorVisible
        {
            get => separator.Visible;
            set => separator.Visible = value;
        }

        public override void _Ready()
        {
            AddChild(separator);

            var titleMargin = new MarginContainer();
            titleMargin.AddConstantOverride("margin_left", 10);

            titleMargin.AddChild(titleLabel);
            AddChild(titleMargin);

            AddChild(container);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationTranslationChanged)
                titleLabel.Text = title.ToString();
        }

        public void EmplaceLabel(InspectedEntityLabel label)
        {
            container.AddChild(label);
            ++totalEntityLabels;
        }

        public void ClearEntries()
        {
            container.FreeChildren();
            totalEntityLabels = 0;
        }
    }
}
