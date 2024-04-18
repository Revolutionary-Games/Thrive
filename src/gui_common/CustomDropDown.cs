using System.Collections.Generic;
using Godot;

/// <summary>
///   A custom dropdown implemented through MenuButton, with extra popup menu functionality
///   such as some slide down animation.
/// </summary>
/// <remarks>
///   <para>
///     In Godot 4 it is possible to set dropdown icon size now so this just has the animation and some data helpers.
///   </para>
/// </remarks>
public partial class CustomDropDown : MenuButton
{
#pragma warning disable CA2213
    /// <summary>
    ///   The MenuButton's popup menu
    /// </summary>
    public readonly PopupMenu Popup;

#pragma warning restore CA2213

    private readonly StringName vSeparationReference = new("v_separation");
    private readonly NodePath themeVSeparationReference = new("theme_override_constants/v_separation");

    private readonly float cachedPopupVSeparation;
    private readonly float fontHeight;
    private readonly float contentMarginTop;

    private readonly Dictionary<string, List<Item>> items = new();

    /// <summary>
    ///   All item icon sizes will be adjusted according to this. Currently, it's automatically
    ///   set according to the PopupMenu's check icon size (with a bit smaller result)
    /// </summary>
    private int iconMaxWidth;

    public CustomDropDown()
    {
        Popup = GetPopup();

        cachedPopupVSeparation = Popup.GetThemeConstant(vSeparationReference);
        fontHeight = Popup.GetThemeFont("font").GetHeight(Popup.GetThemeFontSize("font_size"));
        contentMarginTop = Popup.GetThemeStylebox("panel").ContentMarginTop;

        var checkSize = Popup.GetThemeIcon("checked").GetSize();

        // Set the custom icon size
        iconMaxWidth = (int)(checkSize.X - 2);

        ClipContents = true;

        Connect(MenuButton.SignalName.AboutToPopup, new Callable(this, nameof(OnPopupAboutToShow)));
    }

    public override void _Draw()
    {
        ReadjustRectSizes();
    }

    public void AddItemSection(string name)
    {
        if (!items.ContainsKey(name))
            items.Add(name, new List<Item>());
    }

    /// <summary>
    ///   Helper for adding an item into the items dictionary. This does not add the item into the PopupMenu,
    ///   for that see <see cref="CreateElements"/>.
    /// </summary>
    /// <returns>
    ///   The CustomDropDown's own Item class. All custom operations relating to the dropdown uses this.
    /// </returns>
    public Item AddItem(string text, bool checkable, Color color, Texture2D? icon = null,
        string section = "default")
    {
        if (!items.ContainsKey(section))
        {
            AddItemSection(section);
            items[section].Add(new Item { Text = section, Separator = true });
        }

        var item = new Item
        {
            Text = text,
            Icon = icon,
            Color = color,
            Checkable = checkable,
        };

        items[section].Add(item);

        return item;
    }

    /// <summary>
    ///   Helper for clearing the items in the dictionary.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This method doesn't cause rebuild of the popup.
    ///   </para>
    /// </remarks>
    public void ClearAllItems()
    {
        items.Clear();
    }

    /// <summary>
    ///   Returns the index of an item containing the given name/text in a section.
    /// </summary>
    /// <param name="name">The item text</param>
    /// <param name="section">The item section where to search the item index for</param>
    /// <returns>Item's index. -1 if not found</returns>
    public int GetItemIndex(string name, string section)
    {
        if (!items.TryGetValue(section, out var sectionItems))
        {
            GD.PrintErr("No section found with name ", section);
            return -1;
        }

        foreach (var item in sectionItems)
        {
            if (item.Text == name)
                return Popup.GetItemIndex(item.Id) + 1;
        }

        return -1;
    }

    /// <summary>
    ///   Returns the index of an item containing the given name/text in all section.
    /// </summary>
    /// <param name="name">The item text</param>
    /// <returns>
    ///   List of item's index as this takes into account all exact name occurrences in all sections.
    ///   Empty list if not found.
    /// </returns>
    public List<int> GetItemIndex(string name)
    {
        var result = new List<int>();

        foreach (var section in items)
        {
            foreach (var item in section.Value)
            {
                if (item.Text == name)
                    result.Add(Popup.GetItemIndex(item.Id) + 1);
            }
        }

        return result;
    }

    /// <summary>
    ///   Retrieves all items from dictionary and instantiates them into <see cref="Popup"/>.
    /// </summary>
    public void CreateElements()
    {
        Popup.Clear();

        var id = 0;

        foreach (var section in items)
        {
            foreach (var item in section.Value)
            {
                if (item.Text == "default" && item.Separator)
                    continue;

                item.Id = id++;

                if (item.Icon != null)
                {
                    if (item.Checkable)
                    {
                        Popup.AddIconCheckItem(item.Icon, item.Text, id);
                        var index = Popup.GetItemIndex(id);
                        Popup.SetItemIconMaxWidth(index, iconMaxWidth);
                        Popup.SetItemIconModulate(index, item.Color);
                        Popup.SetItemChecked(index, item.Checked);
                    }
                    else
                    {
                        Popup.AddIconItem(item.Icon, item.Text, id);
                        var index = Popup.GetItemIndex(id);
                        Popup.SetItemIconMaxWidth(index, iconMaxWidth);
                        Popup.SetItemIconModulate(index, item.Color);
                        Popup.SetItemAsSeparator(index, item.Separator);
                    }
                }
                else
                {
                    if (item.Checkable)
                    {
                        Popup.AddCheckItem(item.Text, id);
                        Popup.SetItemChecked(Popup.GetItemIndex(id), item.Checked);
                    }
                    else
                    {
                        Popup.AddItem(item.Text, id);
                        Popup.SetItemAsSeparator(Popup.GetItemIndex(id), item.Separator);
                    }
                }
            }
        }

        ReadjustRectSizes();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            vSeparationReference.Dispose();
            themeVSeparationReference.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   This re-adjust the rect size of this MenuButton and its PopupMenu. Called when they are updated.
    /// </summary>
    private void ReadjustRectSizes()
    {
        // Adjust the menu button to have the same length as the popup
        CustomMinimumSize = new Vector2(Popup.GetContentsMinimumSize().X + iconMaxWidth + 6, CustomMinimumSize.Y);

        // Set popup to minimum length
        Popup.Size = new Vector2I(Mathf.RoundToInt(Size.X), 0);
    }

    private void OnPopupAboutToShow()
    {
        Popup.AddThemeConstantOverride(vSeparationReference, -14);

        // Animate slide down

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(Popup, themeVSeparationReference, cachedPopupVSeparation, 0.1).From(-14);
    }

    /// <summary>
    ///   Helper data regarding the popup menu item. All custom operations relating to the dropdown uses this,
    ///   we can't utilize PopupMenu's internal item class since it's not exposed to the user.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: Fields may not always be updated, especially if the user bypass custom methods and directly
    ///     change the internal items by using methods in <see cref="Popup"/>.
    ///   </para>
    /// </remarks>
    public class Item
    {
        public string Text = string.Empty;
        public Texture2D? Icon;
        public Color Color;
        public bool Checkable;
        public bool Checked;
        public int Id;
        public bool Separator;
    }
}
