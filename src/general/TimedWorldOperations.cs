using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles running IWorldEffect types
/// </summary>
public class TimedWorldOperations
{
    /// <summary>
    ///   This probably needs to be changed to a huge precision number
    ///   depending on what timespans we'll end up using.
    /// </summary>
    private double totalPassedTime = 0;

    private List<IWorldEffect> effects = new List<IWorldEffect>();

    public TimedWorldOperations()
    {
    }

    /// <summary>
    /// Called when time passes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is different than realtime gameplay time, these are
    ///     mostly the time jumps that happen in the editor.
    ///   </para>
    /// </remarks>
    /// <param name="timePassed">Time passed since last call</param>
    public void OnTimePassed(double timePassed)
    {
        totalPassedTime += timePassed;

        GD.Print("TimedWorldOperations: running effects. elapsed: ",
            timePassed, " total passed: ", totalPassedTime);

        foreach (var effect in effects)
        {
            effect.OnTimePassed(timePassed, totalPassedTime);
        }
    }

    /// <summary>
    /// Registers an effect to run when time passes
    /// </summary>
    public void RegisterEffect(string name, IWorldEffect effect)
    {
        effect.OnRegisterToWorld();
        effects.Add(effect);
    }
}
