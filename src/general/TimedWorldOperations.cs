using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    ///   Called when time passes (long timespans, like entering the editor)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is different from realtime gameplay time, these are mostly the time jumps that happen in the editor.
    ///   </para>
    /// </remarks>
    /// <param name="timePassed">Time passed since last call</param>
    /// <param name="totalPassed">Total time passed</param>
    public void OnTimePassed(double timePassed, double totalPassed)
    {
        GD.Print("TimedWorldOperations: running effects. elapsed: ", timePassed, " total passed: ", totalPassed);

        foreach (var effect in effects)
        {
            try
            {
                effect.OnTimePassed(timePassed, totalPassed);
            }
            catch (Exception e)
            {
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();

                throw;
#endif

                // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162 // Unreachable code detected

                GD.PrintErr("Error in applying a world effect! Some timed effect will not apply properly. " +
                    "Exception: " + e);

                // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162 // Unreachable code detected
            }
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
