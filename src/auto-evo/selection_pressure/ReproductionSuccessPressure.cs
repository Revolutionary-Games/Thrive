namespace AutoEvo;

using System;
using SharedBase.Archive;

public class ReproductionSuccessPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("REPRODUCTION_SUCCESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public ReproductionSuccessPressure(float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.HasBindingFeature),
            new AddOrganelleAnywhere(organelle => organelle.HasSignalingFeature),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, -100.0f),
        ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EnergyConsumptionPressure;

    public static ReproductionSuccessPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new ReproductionSuccessPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is MicrobeSpecies)
        {
            return 1.0f;
        }

        if (species is not MulticellularSpecies multicellularSpecies)
        {
            throw new ArgumentException("Wrong type of Species passed to Microbe/Multicellular Species miche tree");
        }

        var score = 1.0f;

        var reproductionMethod = multicellularSpecies.ReproductionMethod;
        if (reproductionMethod == MulticellularReproductionMethod.Budding)
        {
            return 1.0f;
        }
        else if (reproductionMethod == MulticellularReproductionMethod.MassBudding)
        {
            score += 0.3f * ((float)multicellularSpecies.MassBuddingCellCount /
                multicellularSpecies.ModifiableEditorCells.Count);
        }
        else if (reproductionMethod == MulticellularReproductionMethod.Sporulation)
        {
            var modifiableSporeCellType = multicellularSpecies.ModifiableSporeCellType;

            // Logically, this should use an exception instead, but I would expect that to cause problems with in-editor
            // auto-evo prediction before you select a cellType.
            if (modifiableSporeCellType == null)
                return 0.0f;

            var speed = MicrobeInternalCalculations.CalculateSpeed(modifiableSporeCellType.ModifiableOrganelles,
                modifiableSporeCellType.MembraneType, modifiableSporeCellType.MembraneRigidity,
                modifiableSporeCellType.IsBacteria, modifiableSporeCellType.CellTypeSpecializationBonus, true);
            var nominalStorageCapacity = MicrobeInternalCalculations.GetTotalNominalCapacity(
                modifiableSporeCellType.ModifiableOrganelles, modifiableSporeCellType.CellTypeSpecializationBonus);

            // Diminishing returns on storage
            nominalStorageCapacity = (MathF.Pow(nominalStorageCapacity + 1, 0.8f) - 1) / 16f;

            score += Math.Min(speed, 1) * 0.1f;
            score += Math.Min(nominalStorageCapacity, 1) * 0.1f;
        }
        else if (reproductionMethod == MulticellularReproductionMethod.SexualIsogamy)
        {
            var modifiableGameteCellType = multicellularSpecies.ModifiableGameteTypeA;

            // Logically, this should use an exception instead, but I would expect that to cause problems with in-editor
            // auto-evo prediction before you select a cellType.
            if (modifiableGameteCellType == null)
                return 0.0f;

            var speed = MicrobeInternalCalculations.CalculateSpeed(modifiableGameteCellType.ModifiableOrganelles,
                modifiableGameteCellType.MembraneType, modifiableGameteCellType.MembraneRigidity,
                modifiableGameteCellType.IsBacteria, modifiableGameteCellType.CellTypeSpecializationBonus, true);

            var nominalStorageCapacity = MicrobeInternalCalculations.GetTotalNominalCapacity(
                modifiableGameteCellType.ModifiableOrganelles, modifiableGameteCellType.CellTypeSpecializationBonus);

            // Diminishing returns on storage
            nominalStorageCapacity = (MathF.Pow(nominalStorageCapacity + 1, 0.8f) - 1) / 16f;

            score += Math.Min(speed, 1) * 0.1f;
            score += Math.Min(nominalStorageCapacity, 1) * 0.1f;
        }
        else if (reproductionMethod == MulticellularReproductionMethod.SexualAnisogamy)
        {
            var modifiableGameteCellTypeA = multicellularSpecies.ModifiableGameteTypeA;
            var modifiableGameteCellTypeB = multicellularSpecies.ModifiableGameteTypeB;

            // Logically, this should use an exception instead, but I would expect that to cause problems with in-editor
            // auto-evo prediction before you select a cellType.
            if (modifiableGameteCellTypeA == null)
                return 0.0f;
            if (modifiableGameteCellTypeB == null)
                return 0.0f;

            var speedA = MicrobeInternalCalculations.CalculateSpeed(modifiableGameteCellTypeA.ModifiableOrganelles,
                modifiableGameteCellTypeA.MembraneType, modifiableGameteCellTypeA.MembraneRigidity,
                modifiableGameteCellTypeA.IsBacteria, modifiableGameteCellTypeA.CellTypeSpecializationBonus, true);
            var speedB = MicrobeInternalCalculations.CalculateSpeed(modifiableGameteCellTypeB.ModifiableOrganelles,
                modifiableGameteCellTypeB.MembraneType, modifiableGameteCellTypeB.MembraneRigidity,
                modifiableGameteCellTypeB.IsBacteria, modifiableGameteCellTypeB.CellTypeSpecializationBonus, true);
            var speed = Math.Max(speedA, speedB);

            var nominalStorageCapacityA = MicrobeInternalCalculations.GetTotalNominalCapacity(
                modifiableGameteCellTypeA.ModifiableOrganelles, modifiableGameteCellTypeA.CellTypeSpecializationBonus);
            var nominalStorageCapacityB = MicrobeInternalCalculations.GetTotalNominalCapacity(
                modifiableGameteCellTypeB.ModifiableOrganelles, modifiableGameteCellTypeB.CellTypeSpecializationBonus);
            var nominalStorageCapacity = 0.5f * (nominalStorageCapacityA + nominalStorageCapacityB);

            score += Math.Min(speed, 1) * 0.1f;
            score += Math.Min(nominalStorageCapacity, 1) * 0.1f;
        }

        return 1.0f + score;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
