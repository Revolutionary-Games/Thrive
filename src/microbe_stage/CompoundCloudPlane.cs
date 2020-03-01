using System;
using Godot;

public class CompoundCloudPlane : CSGMesh
{
    private Image image;
    private ImageTexture texture;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        int size = Constants.Instance.CLOUD_SIMULATION_WIDTH;
        image = new Image();
        image.Create(size, size, false, Image.Format.Rgba8);
        texture = new ImageTexture();
        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter);

        image.Lock();

        var data = image.GetData();

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i + 0] = 196;
            data[i + 1] = 0;
            data[i + 2] = 0;
            data[i + 3] = 0;
        }

        image.Unlock();

        SetCloudColours(new Color(0.786f, 0.211f, 0.98f, 1.0f),
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 0));
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

        var data = image.GetData();

        // Array.Copy(, data, data.Length);

        image.Unlock();
    }
}
