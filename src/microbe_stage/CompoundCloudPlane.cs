using System;
using Godot;

public class CompoundCloudPlane : CSGMesh
{
    private Image image;
    private ImageTexture texture;

    private int size;

    private float[,] densities1;
    private float[,] densities2;
    private float[,] densities3;
    private float[,] densities4;

    private Compound compound1;
    private Compound compound2;
    private Compound compound3;
    private Compound compound4;

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

        var material = (ShaderMaterial)this.Material;
        material.SetShaderParam("densities", texture);
    }

    /// <summary>
    ///   Initializes this cloud. The unset colour channels are not used
    /// </summary>
    public void Init(Compound cloud1, Compound cloud2, Compound cloud3, Compound cloud4)
    {
        // For simplicity all the densities always exist, even on the unused clouds
        densities1 = new float[size, size];
        densities2 = new float[size, size];
        densities3 = new float[size, size];
        densities4 = new float[size, size];

        compound1 = cloud1;
        compound2 = cloud2;
        compound3 = cloud3;
        compound4 = cloud4;

        var material = (ShaderMaterial)this.Material;

        material.SetShaderParam("colour1", compound1.Colour);

        var blank = new Color(0, 0, 0, 0);

        material.SetShaderParam("colour2", compound2 != null ? compound2.Colour : blank);
        material.SetShaderParam("colour3", compound3 != null ? compound3.Colour : blank);
        material.SetShaderParam("colour4", compound4 != null ? compound4.Colour : blank);
    }

    /// <summary>
    ///   Applies diffuse and advect for this single cloud
    /// </summary>
    public void UpdateCloud(float delta)
    {
    }

    /// <summary>
    ///   Updates the texture with the new densities
    /// </summary>
    public void UploadTexture()
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
