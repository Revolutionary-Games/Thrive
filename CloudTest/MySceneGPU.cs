﻿using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Vector3 = Godot.Vector3;

[Tool]
public class MySceneGPU : Spatial
{
    public CSGBox cloudbox;
    public Vector3 cloudPosition;
    public ShaderMaterial material;
    public float cloudWidth = 100;
    public float cloudHeight = 100;
    public float cloudDepth = 100;
    public float[,,] cloud;
    public float sigma = 20;
    public float b = 50;
    public float pi = (float)Math.PI;
    public float densityMultiplier = 4;
    // cloud sub-shpaes positions in cloud coordinates
    public List<Vector3> shapeCenters = new List<Vector3>();
    public List<int> shapeSizes = new List<int>();
    public int subShapeNumber = 50;
    public int subShapeMaxRadius = 40;
    public int subShapeMinRadius = 25;

    // Generate the cloudlets's position and size.
    public void GenerateSubShapes()
    {
        var random = new Random();
        for (int i = 0; i < subShapeNumber; i++)
        {
            var size = random.Next(subShapeMinRadius, subShapeMaxRadius);
            var x = random.Next(subShapeMaxRadius, cloudWidth - subShapeMaxRadius);
            var y = random.Next(subShapeMaxRadius, cloudHeight - subShapeMaxRadius);
            var z = random.Next(subShapeMaxRadius, cloudDepth - subShapeMaxRadius);
            shapeSizes.Add(size);
            shapeCenters.Add(new Vector3(x, y, z));
        }

    }
    // Generate the cloud by adding values to its density cube according to a gaussian function
    // Iterates only the cloudlets vicinity

    public float signedDistance(Vector3 point, Vector3 center, int radius)
    {
        return (center - point).Length() - radius;
    }
    public float GaussianDensity(float x)
    {
        var a = 1 / (sigma * Math.Sqrt(2 * Math.PI));
        var result = a * Math.Exp(-Math.Pow(x - b, 2) / (2 * Math.Pow(sigma, 2)));
        return (float)result;
    }
    public AABB createCloudAABB(CSGBox cloud)
    {
        var aabbposition = new Vector3(-cloudbox.Width / 2, -cloudbox.Height / 2, -cloudbox.Depth / 2);
        var aabbsize = new Vector3(cloudbox.Width, cloudbox.Height, cloudbox.Depth);
        var aabend = aabbposition - aabbsize;
        aabbposition = cloud.Transform.Xform(aabbposition);
        aabbsize = cloud.Transform.Translated(-cloud.Translation).Xform(aabbsize);
        var cloudaabb = new AABB(aabbposition, aabbsize);

        return cloudaabb;
    }
    public override void _Ready()
    {
        if (Engine.EditorHint)
        {

            cloud = new float[(int)cloudWidth, (int)cloudHeight, (int)cloudDepth];
            cloudbox = (CSGBox)GetTree().Root.FindNode("CSGBox", true, false);
            cloudWidth = (int)cloudbox.Width;
            cloudHeight = (int)cloudbox.Height;
            cloudDepth = (int)cloudbox.Depth;
            GenerateSubShapes();
            MeshInstance cameraMesh = (MeshInstance)GetTree().Root.FindNode("MeshInstance2", true, false);
            ShaderMaterial mat = (ShaderMaterial)cameraMesh.GetSurfaceMaterial(0);
            material = mat;


            Image texture = new Image();
            var w = subShapeNumber;
            texture.Create(w, 1, false, Image.Format.Rgba8);
            texture.Lock();
            GD.Print(shapeCenters[0], shapeCenters[2]);
            for (int i = 0; i < w; i++)
            {
                GD.Print(shapeCenters[i]);
                Color col = new Color(shapeCenters[i].x / cloudWidth, (float)shapeCenters[i].y / cloudHeight,
                (float)shapeCenters[i].z / cloudDepth,
                (float)shapeSizes[i] / subShapeMaxRadius);
                texture.SetPixel(i, 0, col);

            }
            var subshapes = new ImageTexture();
            subshapes.CreateFromImage(texture);




            var cloudaabb = (createCloudAABB(cloudbox));
            cloudPosition = cloudaabb.Position;
            mat.SetShaderParam("bound_min", cloudaabb.Position);
            mat.SetShaderParam("bound_max", cloudaabb.End);
            mat.SetShaderParam("cloudPosition", cloudPosition);
            mat.SetShaderParam("cloudWidth", cloudWidth);
            mat.SetShaderParam("cloudHeight", cloudHeight);
            mat.SetShaderParam("cloudDepth", cloudDepth);
            mat.SetShaderParam("subshapes", subshapes);
            mat.SetShaderParam("maxSubshapeRad", subShapeMaxRadius);
            mat.SetShaderParam("subshapeNumber", w);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            var cloudaabb = (createCloudAABB(cloudbox));
            cloudPosition = cloudaabb.Position;
            material.SetShaderParam("bound_min", cloudaabb.Position);
            material.SetShaderParam("bound_max", cloudaabb.End);
            material.SetShaderParam("cloudPosition", cloudPosition);

            float sigma = (float)material.GetShaderParam("sigma");
            var b = (float)material.GetShaderParam("b");
            var a = 1 / (sigma * Math.Sqrt(2 * pi));
            var c = Math.Exp(-1 / (2 * Math.Pow(sigma, 2)));
            var d = Math.Log(a, c);
            material.SetShaderParam("d", d);
            material.SetShaderParam("c", c);

            material.SetShaderParam("cloud", cloud);
        }
    }
}
