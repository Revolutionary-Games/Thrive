using System;
using Godot;

public class MembraneType
{
    public string NormalTexture = "res://assets/textures/FresnelGradient.png";
    public string DamagedTexture = "res://assets/textures/FresnelGradientDamaged.png";
    public float MovementFactor = 1.0f;
    public float OsmoregulationFactor = 1.0f;
    public float ResourceAbsorptionFactor = 1.0f;
    public int Hitpoints = 100;
    public float PhysicalResistance = 1.0f;
    public float ToxinResistance = 1.0f;
    public int EditorCost = 50;
    public bool CellWall = false;

    public MembraneType()
    {
    }
}
