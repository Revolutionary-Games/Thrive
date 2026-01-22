namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tells the player about negative ATP balance (but only after the ATP introduction tutorial has triggered)
/// </summary>
public class NegativeAtpBalanceTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "NegativeAtpBalanceTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialNegativeAtpBalanceTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NegativeAtpBalanceTutorialVisible = ShownCurrently;
        gui.HandleShowingATPBarHighlight();
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged:
            {
                if (args is EnergyBalanceEventArgs energyBalanceEventArgs)
                {
                    var energyBalanceInfo = energyBalanceEventArgs.EnergyBalanceInfo;
                    bool isNegativeAtpBalance =
                        energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumption;

                    if (!HasBeenShown && isNegativeAtpBalance && CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                        return true;
                    }
                }

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
