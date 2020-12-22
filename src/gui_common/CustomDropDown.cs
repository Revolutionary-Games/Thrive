using System.Collections.Generic;
using Godot;

/// <summary>
///   A custom dropdown implemented through MenuButton, but with extra popup menu functionality
///   such as adjusted custom icon size with tweakable color and some slide down animation.
///   (Might need to expand this later)
/// </summary>
public class CustomDropDown : MenuButton
{
    /// <summary>
    ///   The MenuButton's popup menu
    /// </summary>
    public PopupMenu Popup;

    private Tween tween;

    /// <summary>
    ///   All item icon sizes will be adjusted according to this. Currently it's automatically
    ///   set according to the PopupMenu's check icon size (with a bit smaller result)
    /// </summary>
    private Vector2 iconSize;

    private float cachedPopupHSeparation;
    private float cachedPopupVSeparation;

    private List<Item> items;

    public CustomDropDown()
    {
        Popup = GetPopup();
        items = new List<Item>();
        tween = new Tween();

        AddChild(tween);

        cachedPopupHSeparation = Popup.GetConstant("hseparation");
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

    /// <summary>
    ///   Helper for adding item into PopupMenu and also an icon to be custom drawn in this class
    /// </summary>
    public void AddItem(string text, int id, bool checkable, Color color, Texture icon = null)
    {
        var item = new Item
        {
            Text = text,
            Icon = icon,
            Color = color,
            Id = id,
            Checkable = checkable,
        };

        items.Add(item);

        if (checkable)
        {
            Popup.AddCheckItem(item.Text, item.Id);
        }
        else
        {
            Popup.AddItem(item.Text, id);
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
        // Set popup to minimum length
        Popup.RectSize = new Vector2(GetContentsMinimumSize().x + iconSize.x + 6, 0);

        // Adjust the menu button to have the same length as the popup
        RectMinSize = new Vector2(Popup.RectSize.x, RectMinSize.y);
    }

    /// <summary>
    ///   A workaround to get PopupMenu icons drawn in an adjusted size.
    /// </summary>
    private void DrawIcons()
    {
        if (!Popup.Visible)
            return;

        var contentMinSize = GetContentsMinimumSize();
        var font = Popup.GetFont("font");

        // Offset from the top
        var height = Popup.GetStylebox("panel").ContentMarginTop + (font.GetHeight() / 2) - (iconSize.y / 2);

        foreach (var item in items)
        {
            // Skip if item has no icon
            if (item.Icon == null)
                continue;

            var position = new Vector2(contentMinSize.x, height);

            Popup.DrawTextureRect(item.Icon, new Rect2(position, iconSize), false, item.Color);

            height += font.GetHeight() + cachedPopupVSeparation;
        }
    }

    /// <summary>
    ///   Returns the size of the popup menu contents
    /// </summary>
    private Vector2 GetContentsMinimumSize()
    {
        var minWidth = 0.0f;
        var minHeight = 0.0f;

        var font = Popup.GetFont("font");

        foreach (var item in items)
        {
            var width = iconSize.x + font.GetStringSize(item.Text).x + cachedPopupHSeparation;
            var height = Mathf.Max(iconSize.y, font.GetHeight()) + cachedPopupVSeparation;

            if (item.Checkable)
                width += Popup.GetIcon("checked").GetWidth();

            minWidth = Mathf.Max(minWidth, width);
            minHeight += height;
        }

        return new Vector2(minWidth, minHeight);
    }

    private void OnPopupAboutToShow()
    {
        Popup.AddConstantOverride("vseparation", -14);

        // Animate slide down
        tween.InterpolateProperty(Popup, "custom_constants/vseparation", -14, cachedPopupVSeparation, 0.1f,
            Tween.TransitionType.Circ, Tween.EaseType.Out);
        tween.Start();
    }

    /// <summary>
    ///   Helper data regarding the popup menu item
    /// </summary>
    private class Item
    {
        public string Text;
        public Texture Icon;
        public Color Color;
        public bool Checkable;
        public int Id;
    }
}
