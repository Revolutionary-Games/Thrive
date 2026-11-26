using System.Collections.Generic;
using GdUnit4;
using Systems;
using static GdUnit4.Assertions;

/// <summary>
///   Tests how organelle processes become the active processes of a cell.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ProcessSystemTests
{
    [TestCase]
    public void ActiveProcessList_RebuildsRatesAndOrderWhilePreservingSpeedControls()
    {
        var parameters = SimulationParameters.Instance;
        var toxinVacuole = new OrganelleTemplate(parameters.GetOrganelleType("oxytoxy"), new Hex(0, 0), 0);
        var cytoplasm = new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"), new Hex(1, 0), 0);
        var secondCytoplasm = new OrganelleTemplate(cytoplasm.Definition, new Hex(2, 0), 0);
        var chloroplast = new OrganelleTemplate(parameters.GetOrganelleType("chloroplast"), new Hex(-1, 0), 0);
        var glycolysis = parameters.GetBioProcess("glycolysis_cytoplasm");
        var toxinSynthesis = parameters.GetBioProcess("cytotoxinSynthesis");
        var cytoplasmRate = cytoplasm.Definition.Processes![glycolysis.InternalName];
        List<TweakedProcess>? activeProcesses = null;

        ProcessSystem.ComputeActiveProcessList([toxinVacuole, cytoplasm, chloroplast], ref activeProcesses);

        AssertThat(activeProcesses).HasSize(3);
        AssertThat(activeProcesses[0].Process).IsSame(toxinSynthesis);
        AssertThat(activeProcesses[1].Process).IsSame(glycolysis);
        AssertThat(activeProcesses[2].Process).IsSame(parameters.GetBioProcess("photosynthesis"));

        var previousList = activeProcesses;
        activeProcesses[1] = new TweakedProcess(glycolysis, cytoplasmRate) { SpeedMultiplier = 0.25f };

        ProcessSystem.ComputeActiveProcessList([cytoplasm, secondCytoplasm, toxinVacuole], ref activeProcesses);

        AssertThat(ReferenceEquals(activeProcesses, previousList)).IsTrue();
        AssertThat(activeProcesses).HasSize(2);
        AssertThat(activeProcesses[0].Process).IsSame(glycolysis);
        AssertThat(activeProcesses[0].Rate).IsEqual(2 * cytoplasmRate);
        AssertThat(activeProcesses[0].SpeedMultiplier).IsEqual(0.25f);
        AssertThat(activeProcesses[1].Process).IsSame(toxinSynthesis);
        AssertThat(activeProcesses[1].Rate).IsEqual(toxinVacuole.Definition.Processes![toxinSynthesis.InternalName]);
    }

    [TestCase]
    public void ActiveProcessList_UsesUpgradeProcessForOnlyTheUpgradedOrganelle()
    {
        var parameters = SimulationParameters.Instance;
        var toxinVacuole = parameters.GetOrganelleType("oxytoxy");
        var standard = new OrganelleTemplate(toxinVacuole, new Hex(0, 0), 0);
        var upgraded = new OrganelleTemplate(toxinVacuole, new Hex(1, 0), 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades { ModifiableUnlockedFeatures = ["oxytoxy"] },
        };
        List<TweakedProcess>? activeProcesses = null;

        ProcessSystem.ComputeActiveProcessList([upgraded, standard], ref activeProcesses);

        AssertThat(activeProcesses).HasSize(2);
        AssertThat(activeProcesses[0].Process).IsSame(parameters.GetBioProcess("oxytoxySynthesis"));
        AssertThat(activeProcesses[1].Process).IsSame(parameters.GetBioProcess("cytotoxinSynthesis"));
    }
}
