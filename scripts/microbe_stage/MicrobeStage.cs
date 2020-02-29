using Godot;
using System;

public class MicrobeStage : Spatial
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SetupStage();
    }

    // Prepares the stage for playing
    // Also begins a new game if one hasn't been started yet for easier debugging
    public void SetupStage(){

    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
