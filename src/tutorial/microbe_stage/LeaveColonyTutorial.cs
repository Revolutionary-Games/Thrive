namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   When full on reproduction compounds, the player needs to realize to leave a non-multicellular colony to
///   progress
/// </summary>
public class LeaveColonyTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private bool hasColony;
    private bool fullAmmonia;
    private bool fullPhosphates;

    public override string ClosedByName => "LeaveColonyTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialLeaveColonyTutorial;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.LeaveColonyTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerColony:
            {
                if (!HasBeenShown)
                {
                    var data = (MicrobeColonyEventArgs)args;

                    // Give advice if player is in a colony, but not big enough to get to the next stage
                    // Also prevent triggering when multicellular to avoid this incorrectly showing if the early
                    // game was played without tutorials
                    hasColony = data.HasColony && data.MemberCount <
                        Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR && !data.IsMulticellular;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerCompounds:
            {
                if (!HasBeenShown && hasColony && CanTrigger)
                {
                    var compounds = ((CompoundBagEventArgs)args).Compounds;

                    fullAmmonia = compounds.GetCapacityForCompound(Compound.Ammonia) -
                        compounds.GetCompoundAmount(Compound.Ammonia) <
                        MathUtils.EPSILON;

                    fullPhosphates = compounds.GetCapacityForCompound(Compound.Phosphates) -
                        compounds.GetCompoundAmount(Compound.Phosphates) <
                        MathUtils.EPSILON;

                    if (fullAmmonia && fullPhosphates && !overallState.TutorialActive())
                        Show();
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                if (ShownCurrently)
                    Inhibit();
                break;
            }
        }

        return false;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
