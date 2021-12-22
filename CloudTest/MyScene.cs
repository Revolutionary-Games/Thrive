using Godot;
using System;

[Tool]
public class MyScene : Spatial
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public void GetNodes(Node nodi)
	{
		//GD.Print(nodi.Name);
		var nodes = nodi.GetChildren();
		if (nodes != null)
		foreach (Node nod in nodes)
		{
			if (nod is Viewport spat){
				GD.Print("\nViewport:"+spat.Name);
				if (spat.GetCamera() != null)
					GD.Print(spat.GetCamera().GetPath()+" Camera:"+spat.GetCamera().GlobalTransform);
			}
			else
				GetNodes(nod);
		}
	}
	public override void _Ready()
	{
		if (Engine.EditorHint)
		{
			// var nodes = GetNode("/root").GetChildren();
			// foreach (Node nod in nodes)
			// {
			// 	if (nod is Spatial spat)
			// 		GD.Print(spat.Name);
			// 	else
			// 		GD.Print(nod.Name);
			// }
			GetNodes(GetNode("/root"));
			GD.Print("GetTree().Root.GetCamera()");
		}
	}

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)
  {
	  if (Engine.EditorHint)
		{
			Spatial camera =  GetNode<Spatial>("Viewport/Camera");
			Camera c1= GetNode<Camera>("/root/EditorNode/@@592/@@593/@@601/@@603/@@607/@@611/@@612/@@613/@@629/@@630/@@639/@@640/@@6539/@@6346/@@6347/@@6348/@@6366/@@6349/@@6350/@@6352");
			Camera c2= GetNode<Camera>("/root/EditorNode/@@592/@@593/@@601/@@603/@@607/@@611/@@612/@@613/@@629/@@630/@@639/@@640/@@6539/@@6346/@@6347/@@6348/@@6384/@@6367/@@6368/@@6370") ;
			Camera c3= GetNode<Camera>("/root/EditorNode/@@592/@@593/@@601/@@603/@@607/@@611/@@612/@@613/@@629/@@630/@@639/@@640/@@6539/@@6346/@@6347/@@6348/@@6402/@@6385/@@6386/@@6388") ;
			Camera c4= GetNode<Camera>("/root/EditorNode/@@592/@@593/@@601/@@603/@@607/@@611/@@612/@@613/@@629/@@630/@@639/@@640/@@6539/@@6346/@@6347/@@6348/@@6420/@@6403/@@6404/@@6406") ;
			camera.GlobalTransform = c1.GlobalTransform;
		}
  }
}
