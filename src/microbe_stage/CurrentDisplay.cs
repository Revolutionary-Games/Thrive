using System;
using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class CurrentDisplay : GpuParticles3D
{
    private float time = 0.0f;

    public override void _Process(double delta)
    {
        base._Process(delta);

        GlobalPosition = new Vector3(GlobalPosition.X, 1.0f, GlobalPosition.Z);

        if (!PauseManager.Instance.Paused)
        {
            time += (float)delta;

            ((ShaderMaterial)ProcessMaterial).SetShaderParameter("game_time", time);
        }
    }
}
