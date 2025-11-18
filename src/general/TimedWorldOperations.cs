using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Handles running IWorldEffect types
/// </summary>
public class TimedWorldOperations : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private List<IWorldEffect> effects = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.TimedWorldOperations;
    public bool CanBeReferencedInArchive => false;

    public static TimedWorldOperations ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new TimedWorldOperations
        {
            effects = reader.ReadObject<List<IWorldEffect>>(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(effects);
    }

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
    public void RegisterEffect(string name, IWorldEffect effect, bool last = true)
    {
        _ = name;
        effect.OnRegisterToWorld();

        if (last)
        {
            effects.Add(effect);
            return;
        }

        effects.Insert(effects.Count - 2, effect);
    }
}
