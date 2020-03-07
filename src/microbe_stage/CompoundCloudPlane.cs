using System;
using Godot;

public class CompoundCloudPlane : CSGMesh
{
    private Image image;
    private ImageTexture texture;

    private int size;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        size = Constants.Instance.CLOUD_SIMULATION_WIDTH;
        image = new Image();
        image.Create(size, size, false, Image.Format.Rgba8);
        texture = new ImageTexture();

        // Blank out the image
        image.Lock();

        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                image.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        image.Unlock();

        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter);

        SetCloudColours(new Color(0.786f, 0.211f, 0.98f, 1.0f),
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 0));

        var material = (ShaderMaterial)this.Material;
        material.SetShaderParam("densities", texture);
    }

    public void SetCloudColours(Color cloud1, Color cloud2, Color cloud3, Color cloud4)
    {
        var material = (ShaderMaterial)this.Material;
        material.SetShaderParam("colour1", cloud1);
        material.SetShaderParam("colour2", cloud2);
        material.SetShaderParam("colour3", cloud3);
        material.SetShaderParam("colour4", cloud4);
    }

    public void UpdateDensities(float[][] densities1, float[][] densities2,
        float[][] densities3, float[][] densities4)
    {
        image.Lock();

        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                image.SetPixel(x, y, new Color(0.852f, 0, 0, 0));
            }
        }

        image.Unlock();

        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter);
    }
}
