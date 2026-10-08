using Godot;

public static class TexturizationHelpers
{
    /// <summary>
    ///   Applies the specified amount of passes of <paramref name="blitMaterial"/> onto the given texture
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Ping-pongs between two drawable textures to complete the passes entirely on GPU
    ///   </para>
    /// </remarks>
    public static Texture2D ApplyBlitMaterial(Texture2D source, Material blitMaterial, int passes,
        Color defaultColour)
    {
        // TODO: reuse ping and pong textures. Note that one of those is returned and modifying it will modify the
        // texture of an actual creature. Either that texture shouldn't be reused, or it should be returned like
        // this: ImageTexture.CreateFromImage(source.GetImage());
        var ping = new DrawableTexture2D();
        ping.Setup(source.GetWidth(), source.GetHeight(), DrawableTexture2D.DrawableFormat.Rgba8, defaultColour);

        DrawableTexture2D pong = null!;
        if (passes > 1)
        {
            pong = new DrawableTexture2D();
            pong.Setup(source.GetWidth(), source.GetHeight(), DrawableTexture2D.DrawableFormat.Rgba8, defaultColour);
        }

        var targetRect = new Rect2I(Vector2I.Zero, new Vector2I(source.GetWidth(), source.GetHeight()));

        for (int pass = 0; pass < passes; ++pass)
        {
            var target = pass % 2 == 0 ? ping : pong;

            target.BlitRect(targetRect, source, material: blitMaterial);

            source = target;
        }

        return source;
    }
}
