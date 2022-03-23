using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Vector3 = Godot.Vector3;
[Tool]

public class MyScene : Spatial
{
	public Vector3 cloudPosition;
	public float cloudWidth = 100;
	public float cloudHeight = 100;
	public float cloudDepth = 100;
	public float[ , , ] cloud;
	public float sigma;
	public float b;
	public float densityMultiplier = 2;
	// cloud sub-shpaes positions in cloud coordinates
	public List<Vector3> shapeCenters = new List<Vector3>();
	public List<int> shapeSizes = new List<int>(); 
	public int subShapeNumber = 30;
	public int subShapeMaxRadius = 30;
	public int subShapeMinRadius = 5;
	public void GenerateSubShapePositions()
	{
		var random = new Random();
		for (int i = 0; i<subShapeNumber; i++)
		{
			var size = random.Next(subShapeMinRadius,subShapeMaxRadius);
			var x = random.Next(-cloudWidth/2,cloudWidth/2);
			var y = random.Next(-cloudHeight/2,cloudHeight/2);
			var z = random.Next(-cloudDepth/2,cloudDepth/2);
			shapeSizes.Add(size);
			shapeCenters.Add(new Vector3(x,y,z));
		}

	}
	// Generate the cloud by adding values to its density cube according to a gaussian function
	public void GenerateCloud()
	{
		cloud = new float[(int)cloudWidth,(int)cloudHeight,(int)cloudDepth];
		for (int i = 0; i<subShapeNumber; i++)
		{
			var shapeRadius = shapeSizes[i];
			var shapeCenter = shapeCenters[i];

			for (int x = (int)shapeCenter.x - shapeRadius; x < (int)shapeCenter.x + shapeRadius; x++)
				for (int y = (int)shapeCenter.y - shapeRadius; y < (int)shapeCenter.x + shapeRadius; y++)
					for (int z = (int)shapeCenter.z - shapeRadius; z < (int)shapeCenter.z + shapeRadius; z++)
					{
						Vector3 position = new Vector3(x,y,z);
						float distanceToEdge =signedDistance(position, shapeCenter, shapeRadius);
						distanceToEdge = Math.Abs(distanceToEdge);
						cloud[x,y,z] += GaussianDensity(distanceToEdge) * distanceToEdge * densityMultiplier;
					}
		}
	}
	public float signedDistance(Vector3 point, Vector3 center, int radius)
	{
		return (center - point).Length() - radius;
	}
	public float GaussianDensity(float x)
	{
		var a = 1/(sigma * Math.Sqrt(2*Math.PI));
		var result = a * Math.Exp(-Math.Pow(x - b, 2)/(2 * Math.Pow(sigma,2)));
		return (float)result;
	}
	public override void _Ready()
	{
		if (Engine.EditorHint)
		{

		}
	}

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)
  {
	  if (Engine.EditorHint)
		{

		}
  }
}
