using Godot;

/// <summary>
///   Custom label with word wrap which dynamically expands to fill space.
/// </summary>
/// <remarks>
///   <para>
///     Due to a Godot bug, ordinary labels with word wrap and the expand fill flag initialise with a width of zero,
///     so each character ends up on a new line. This massively expands the height of the label, which doesn't reset
///     afterwards. See https://github.com/godotengine/godot/issues/47005.
///   </para>
///   <para>
///     This custom class fixes the bug by only enabling word wrap once the label becomes visible.
///   </para>
/// </remarks>
public partial class CustomExpandingWordWrappedLabel : Label
{
    public CustomExpandingWordWrappedLabel(string text)
    {
        Text = text;
        Align = AlignEnum.Center;
        SizeFlagsHorizontal = (int)SizeFlags..XpandFill;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
            Autowrap = true;
        }
    }
}
