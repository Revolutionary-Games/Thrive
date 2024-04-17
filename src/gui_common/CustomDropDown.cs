using System.Collections.Generic;
using Godot;

/// <summary>
///   A custom dropdown implemented through MenuButton, with extra popup menu functionality
///   such as adjusted custom icon size with tweakable color and some slide down animation.
///   (Might need to expand this later)
/// </summary>
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
    private Vector2 iconSize;

    public CustomDropDown()
    {
        Popup = GetPopup();

        cachedPopupVSeparation = Popup.GetThemeConstant(vSeparationReference);
        fontHeight = Popup.GetThemeFont("font").GetHeight(Popup.GetThemeFontSize("font_size"));
        contentMarginTop = Popup.GetThemeStylebox("panel").ContentMarginTop;

        var checkSize = Popup.GetThemeIcon("checked").GetSize();

        // Set the custom icon size
        iconSize = new Vector2(checkSize.X - 2, checkSize.Y - 2);

        ClipContents = true;

        Connect(MenuButton.SignalName.AboutToPopup, new Callable(this, nameof(OnPopupAboutToShow)));

        Connect(CanvasItem.SignalName.Draw, new Callable(this, nameof(RedrawPopup)));
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

        // Redraw the menu button and popup.
        // Godot 4 change:
        // There doesn't seem to be any way to force redraw of the PopupMenu as it creates an internal control
        // in C++ in PopupMenu::PopupMenu in scene/gui/popup_menu.cpp but offers no way to access it, and itself it
        // isn't any kind of object that can be told to re-draw
        // Popup.Control.QueueRedraw();

        QueueRedraw();
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

    private void RedrawPopup()
    {
        ReadjustRectSizes();
        DrawIcons();
    }

    /// <summary>
    ///   This re-adjust the rect size of this MenuButton and its PopupMenu.
    ///   Called when they are to be redrawn.
    /// </summary>
    private void ReadjustRectSizes()
    {
        // Adjust the menu button to have the same length as the popup
        CustomMinimumSize = new Vector2(Popup.GetContentsMinimumSize().X + iconSize.X + 6, CustomMinimumSize.Y);

        // Set popup to minimum length
        Popup.Size = new Vector2I(Mathf.RoundToInt(Size.X), 0);
    }

    /// <summary>
    ///   A workaround to get PopupMenu icons drawn in an adjusted size.
    /// </summary>
    private void DrawIcons()
    {
        if (!Popup.Visible)
            return;

        // Offset from the top
        var height = contentMarginTop + (fontHeight * 0.5f) - (iconSize.Y * 0.5f);

        var separation = Popup.GetThemeConstant(vSeparationReference);

        foreach (var section in items)
        {
            foreach (var item in section.Value)
            {
                if (item.Separator && item.Text != "default")
                {
                    height += fontHeight + separation;
                    continue;
                }

                // Skip if item has no icon
                if (item.Icon == null)
                    continue;

                var position = new Vector2(Popup.Size.X - iconSize.X - 6, height);

                // TODO: this used to use Popup.DrawTextureRect but Popup is no longer a control that can draw stuff
                // See the comment about QueueRedraw() problems with the new Popup implementation in this file
                DrawTextureRect(item.Icon, new Rect2(position, iconSize), false, item.Color);

                height += fontHeight + separation;
            }
        }
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
