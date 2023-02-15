using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles running IWorldEffect types
/// </summary>
public class TimedWorldOperations
{
    [JsonProperty]
    private List<IWorldEffect> effects = new();

    /// <summary>
    ///   Called when time passes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is different than realtime gameplay time, these are
    ///     mostly the time jumps that happen in the editor.
    ///   </para>
    /// </remarks>
    /// <param name="timePassed">Time passed since last call</param>
    /// <param name="totalPassed">Total time passed</param>
    public void OnTimePassed(double timePassed, double totalPassed)
    {
        GD.Print("TimedWorldOperations: running effects. elapsed: ",
            timePassed, " total passed: ", totalPassed);

        foreach (var effect in effects)
        {
            effect.OnTimePassed(timePassed, totalPassed);
        }
    }

    /// <summary>
    ///   Registers an effect to run when time passes
    /// </summary>
    public void RegisterEffect(string name, IWorldEffect effect)
    {
        _ = name;
        effect.OnRegisterToWorld();
        effects.Add(effect);
    }
}
