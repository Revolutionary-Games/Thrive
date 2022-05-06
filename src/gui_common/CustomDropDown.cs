﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   A custom dropdown implemented through MenuButton, with extra popup menu functionality
///   such as adjusted custom icon size with tweakable color and some slide down animation.
///   (Might need to expand this later)
/// </summary>
public class CustomDropDown : MenuButton
{
    /// <summary>
    ///   The MenuButton's popup menu
    /// </summary>
    public PopupMenu Popup;

    private readonly float cachedPopupVSeparation;

    private Tween tween;

    /// <summary>
    ///   All item icon sizes will be adjusted according to this. Currently it's automatically
    ///   set according to the PopupMenu's check icon size (with a bit smaller result)
    /// </summary>
    private Vector2 iconSize;

    private Dictionary<string, List<Item>> items;

    public CustomDropDown()
    {
        Popup = GetPopup();
        items = new Dictionary<string, List<Item>>();
        tween = new Tween();

        AddChild(tween);

        cachedPopupVSeparation = Popup.GetConstant("vseparation");

        var checkSize = Popup.GetIcon("checked").GetSize();

        // Set the custom icon size
        iconSize = new Vector2(checkSize.x - 2, checkSize.y - 2);

        Popup.RectClipContent = true;

        Connect("about_to_show", this, nameof(OnPopupAboutToShow));
        Popup.Connect("draw", this, nameof(RedrawPopup));
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
    public Item AddItem(string text, bool checkable, Color color, Texture? icon = null,
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

        // Redraw the menu button and popup
        Popup.Update();
        Update();
    }

    private void RedrawPopup()
    {
        ReadjustRectSizes();
        DrawIcons();
    }

    /// <summary>
    ///   This readjust the rect size of this MenuButton and its PopupMenu.
    ///   Called when they are to be redrawn.
    /// </summary>
    private void ReadjustRectSizes()
    {
        // Adjust the menu button to have the same length as the popup
        RectMinSize = new Vector2(Popup.GetMinimumSize().x + iconSize.x + 6, RectMinSize.y);

        // Set popup to minimum length
        Popup.RectSize = new Vector2(RectSize.x, 0);
    }

    /// <summary>
    ///   A workaround to get PopupMenu icons drawn in an adjusted size.
    /// </summary>
    private void DrawIcons()
    {
        if (!Popup.Visible)
            return;

        var font = Popup.GetFont("font");

        // Offset from the top
        var height = Popup.GetStylebox("panel").ContentMarginTop + (font.GetHeight() / 2) - (iconSize.y / 2);

        foreach (var section in items)
        {
            foreach (var item in section.Value)
            {
                if (item.Separator && item.Text != "default")
                {
                    height += font.GetHeight() + Popup.GetConstant("vseparation");
                    continue;
                }

                // Skip if item has no icon
                if (item.Icon == null)
                    continue;

                var position = new Vector2(Popup.RectSize.x - iconSize.x - 6, height);

                Popup.DrawTextureRect(item.Icon, new Rect2(position, iconSize), false, item.Color);

                height += font.GetHeight() + Popup.GetConstant("vseparation");
            }
        }
    }

    private void OnPopupAboutToShow()
    {
        Popup.AddConstantOverride("vseparation", -14);

        // Animate slide down
        tween.InterpolateProperty(Popup, "custom_constants/vseparation", -14, cachedPopupVSeparation, 0.1f,
            Tween.TransitionType.Cubic, Tween.EaseType.Out);
        tween.Start();
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
        public Texture? Icon;
        public Color Color;
        public bool Checkable;
        public bool Checked;
        public int Id;
        public bool Separator;
    }
}
