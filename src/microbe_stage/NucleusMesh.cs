using Godot;

public class NucleusMesh : MeshInstance
{
    public override void _Ready()
    {
        // Setting the material of the ER and Golgi.
        // If the Nucleus, Golgi and/or ER get a texture they should be set here.
        GetChild<MeshInstance>(0).MaterialOverride = MaterialOverride;
        GetChild<MeshInstance>(1).MaterialOverride = MaterialOverride;
    }
}
