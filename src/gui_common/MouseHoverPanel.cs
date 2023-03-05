using Godot;
using Godot.Collections;

/// <summary>
///   The panel that shows what the player is hovering over/inspecting.
/// </summary>
public class MouseHoverPanel : PanelContainer
{
    [Export]
    public NodePath CategoriesContainerPath = null!;

    [Export]
    public NodePath NothingHereContainerPath = null!;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private Container categoriesContainer = null!;
    private Container nothingHereContainer = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private System.Collections.Generic.Dictionary<string, MouseHoverCategory> categories = new();
    private Array categoriesOrdered = new();

    public override void _Ready()
    {
        categoriesContainer = GetNode<Container>(CategoriesContainerPath);
        nothingHereContainer = GetNode<Container>(NothingHereContainerPath);
    }

    public override void _Process(float delta)
    {
        var visibleEntriesCount = 0;

        MouseHoverCategory? firstVisibleCategory = null;

        foreach (MouseHoverCategory category in categoriesOrdered)
        {
            category.Visible = category.TotalEntriesCount > 0;
            category.SeparatorVisible = true;
            visibleEntriesCount += category.TotalEntriesCount;

            if (firstVisibleCategory == null && category.Visible)
                firstVisibleCategory = category;
        }

        if (firstVisibleCategory != null)
            firstVisibleCategory.SeparatorVisible = false;

        nothingHereContainer.Visible = visibleEntriesCount <= 0;
    }

    public void AddCategory(string internalName, LocalizedString displayName)
    {
        if (categories.ContainsKey(internalName))
        {
            GD.Print("MouseHoverPanel: Category already exist for \"", internalName, "\"");
            return;
        }

        var categoryControl = new MouseHoverCategory(displayName);
        categories.Add(internalName, categoryControl);
        categoriesContainer.AddChild(categoryControl);

        categoriesOrdered = categoriesContainer.GetChildren();
    }

    public void MoveCategory(string internalName, int position)
    {
        if (!categories.TryGetValue(internalName, out MouseHoverCategory categoryControl))
        {
            GD.Print("MouseHoverPanel: Category doesn't exist for \"", internalName, "\"");
            return;
        }

        categoriesContainer.MoveChild(categoryControl, position);

        categoriesOrdered = categoriesContainer.GetChildren();
    }

    public InspectedEntityLabel? AddItem(string category, string title, Texture? icon = null)
    {
        if (!categories.TryGetValue(category, out MouseHoverCategory categoryControl))
        {
            GD.Print("MouseHoverPanel: Can't add item, category doesn't exist for \"", category, "\"");
            return null;
        }

        var label = new InspectedEntityLabel(title, icon);
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
            CategoriesContainerPath.Dispose();
            NothingHereContainerPath.Dispose();
            categoriesOrdered.Dispose();
        }
    }

    public class MouseHoverCategory : VBoxContainer
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private Label titleLabel;
        private VBoxContainer container;
        private HSeparator separator;
#pragma warning restore CA2213 // Disposable fields should be disposed

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
                var totalVisible = 0;

                foreach (Control child in container.GetChildren())
                {
                    if (child.Visible)
                        ++totalVisible;
                }

                return totalVisible;
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
        }

        public void ClearEntries()
        {
            container.FreeChildren();
        }
    }
}
