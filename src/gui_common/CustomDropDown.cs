using System.Collections.Generic;
using Godot;

/// <summary>
///   Basically a class that inherits MenuButton, but with extra popup menu functionality such as
///   adjustable icon size and some slide down animation. (Might need to expand this later)
/// </summary>
public class CustomDropDown : MenuButton
{
    /// <summary>
    ///   All item icon sizes will be adjusted according to this
    /// </summary>
    public Vector2 IconSize;

    /// <summary>
    ///   The MenuButton's popup menu
    /// </summary>
    public PopupMenu Popup;

    private Tween tween;

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

        Connect("about_to_show", this, nameof(OnPopupAboutToShow));
        Popup.Connect("draw", this, nameof(RedrawPopup));
    }

    /// <summary>
    ///   Helper for adding item into PopupMenu and also an icon to be custom drawn here
    /// </summary>
    public void AddItem(string name, int id, bool checkable, Color color, Texture icon = null)
    {
        var item = new Item
        {
            Text = name,
            Icon = icon,
            Color = color,
            Id = id,
            Checkable = checkable,
        };

        items.Add(item);

        if (checkable)
        {
            Popup.AddCheckItem(name, id);
        }
        else
        {
            Popup.AddItem(name, id);
        }

        // Waits until next frame, this fixes an incorrect rect size for some reason
        Invoke.Instance.Queue(() => Popup.EmitSignal("draw"));
    }

    private void RedrawPopup()
    {
        // Set popup to minimum length
        Popup.RectSize = new Vector2(GetContentsMinimumSize().x + IconSize.x + 6, 0);

        // Adjust the menu button to have the same length as the popup
        RectMinSize = new Vector2(Popup.RectSize.x, RectMinSize.y);

        DrawIcons();
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

        var height = (Popup.RectSize.y - 10) - contentMinSize.y;

        foreach (var item in items)
        {
            // Skip if item has no icon
            if (item.Icon == null)
                continue;

            var rect = new Rect2(new Vector2(contentMinSize.x, height), IconSize);

            GetPopup().DrawTextureRect(item.Icon, rect, false, item.Color);

            height += font.GetStringSize(item.Text).y + cachedPopupVSeparation;
        }

        // TODO: fix icons drawn outside popup area
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
            var width = IconSize.x + font.GetStringSize(item.Text).x + cachedPopupHSeparation;

            if (item.Checkable)
                width += Popup.GetIcon("checked").GetWidth();

            var height = Mathf.Max(IconSize.y, font.GetStringSize(item.Text).y) + cachedPopupVSeparation;

            minWidth = Mathf.Max(minWidth, width);
            minHeight += height;
        }

        return new Vector2(minWidth, minHeight);
    }

    private void OnPopupAboutToShow()
    {
        Popup.AddConstantOverride("vseparation", -14);

        // Animate slide down
        tween.InterpolateProperty(Popup, "custom_constants/vseparation", -14, cachedPopupVSeparation, 0.3f,
            Tween.TransitionType.Circ, Tween.EaseType.Out);
        tween.Start();
    }

    private struct Item
    {
        public string Text;
        public Texture Icon;
        public Color Color;
        public bool Checkable;
        public int Id;
    }
}
