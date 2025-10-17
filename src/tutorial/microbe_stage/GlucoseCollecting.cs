namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Tutorial pointing glucose collecting out to the player
/// </summary>
public class GlucoseCollecting : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private Vector3? glucosePosition;

    /// <summary>
    ///   Holds the next tutorial we should notify we are done.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: this one property makes saving that one tutorial a bit awkward so refactoring this usage would be
    ///     pretty nice
    ///   </para>
    /// </remarks>
    private MicrobeReproduction? nextTutorial;

    public GlucoseCollecting()
    {
        UsesPlayerPositionGuidance = true;
        CanTrigger = false;
    }

    [JsonIgnore]
    public HUDBottomBar? HUDBottomBar { get; set; }

    [JsonIgnore]
    public CompoundPanels? CompoundPanels { get; set; }

    public override string ClosedByName => "GlucoseCollecting";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialGlucoseCollecting;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.GlucoseTutorialVisible = ShownCurrently;
    }

    public override void Hide()
    {
        // Whenever this is hidden, we want to let the next tutorial know it can start
        if (ShownCurrently)
        {
            if (nextTutorial != null)
            {
                nextTutorial.ReportPreviousTutorialComplete();
                nextTutorial = null;
            }
        }

        base.Hide();
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerCompounds:
            {
                var compounds = ((CompoundBagEventArgs)args).Compounds;

                if (!HasBeenShown && !CanTrigger &&
                    compounds.GetCompoundAmount(Compound.Glucose) < compounds.GetCapacityForCompound(Compound.Glucose) -
                    Constants.GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE)
                {
                    CanTrigger = true;
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobeCompoundsNearPlayer:
            {
                var data = (EntityPositionEventArgs)args;

                if (!HasBeenShown && data.EntityPosition.HasValue && CanTrigger && !overallState.TutorialActive())
                {
                    if (CompoundPanels != null && HUDBottomBar != null)
                    {
                        CompoundPanels.ShowPanel = true;
                        HUDBottomBar.CompoundsPressed = true;
                    }
                    else
                    {
                        GD.PrintErr("Missing GUI panels in glucose tutorial");
                    }

                    nextTutorial = overallState.MicrobeReproduction;
                    Show();
                }

                if (ShownCurrently)
                {
                    glucosePosition = data.EntityPosition;
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerTotalCollected:
            {
                if (!ShownCurrently)
                    break;

                var compounds = ((CompoundEventArgs)args).Compounds;

                if (compounds.TryGetValue(Compound.Glucose, out var amount) &&
                    amount >= Constants.GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE)
                {
                    // Tutorial is now complete
                    Hide();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    public override Vector3? GetPositionGuidance()
    {
        return glucosePosition;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(glucosePosition != null);
        if (glucosePosition != null)
        {
            writer.Write(glucosePosition.Value);
        }

        if (nextTutorial != null)
        {
            writer.WriteObject(nextTutorial);
        }
        else
        {
            writer.WriteNullObject();
        }
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);

        var hasPosition = reader.ReadBool();
        glucosePosition = hasPosition ? reader.ReadVector3() : null;
        nextTutorial = reader.ReadObjectOrNull<MicrobeReproduction>();
    }
}
