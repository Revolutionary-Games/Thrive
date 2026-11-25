namespace AutoEvo;

using System;
using SharedBase.Archive;

/// <summary>
///   Selection pressure that rewards multicellular species for dividing labour between their cell types instead of
///   growing a colony of identical cells. Species score higher the more distinct cell types they use in their body
///   plan and the more distinct functional roles those cell types cover.
/// </summary>
/// <remarks>
///   <para>
///     This is deliberately independent of <see cref="ICellDefinition.CellTypeSpecializationBonus"/>, because that
///     multiplier already boosts process rates and movement directly. This pressure instead measures the structural
///     diversity of the body plan, which no other pressure currently rewards, and is a step towards the planned
///     tissue differentiation of the multicellular and macroscopic stages.
///   </para>
/// </remarks>
public class CellDivisionOfLabourPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   How many distinct cell types can contribute a bonus. Values above this are treated as this value.
    /// </summary>
    public const int MAX_BONUSED_CELL_TYPES = 3;

    /// <summary>
    ///   Score added per extra cell type beyond the stem cell type.
    /// </summary>
    public const float CELL_TYPE_BONUS = 0.25f;

    /// <summary>
    ///   Score added per distinct functional role covered by the body plan.
    /// </summary>
    public const float ROLE_BONUS = 0.2f;

    /// <summary>
    ///   Score of a species that has no division of labour at all (including all single-celled species).
    /// </summary>
    public const float BASELINE_SCORE = 1.0f;

    /// <summary>
    ///   Score removed for every tissue block beyond one per cell type, as scattered tissues are less useful than
    ///   consolidated ones.
    /// </summary>
    public const float FRAGMENTATION_PENALTY = 0.1f;

    /// <summary>
    ///   Maximum total fragmentation penalty so that a messy body plan can never fall below the baseline score.
    /// </summary>
    public const float MAX_FRAGMENTATION_PENALTY = 0.5f;

    /// <summary>
    ///   Highest score this pressure can ever return.
    /// </summary>
    public const float MAX_SCORE = BASELINE_SCORE + MAX_BONUSED_CELL_TYPES * CELL_TYPE_BONUS + 5 * ROLE_BONUS;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_CELL_DIVISION_OF_LABOUR_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public CellDivisionOfLabourPressure(float weight) : base(weight, [
        new AddCellWithOrganelle(organelle => organelle.HasMovementComponent),
        new AddCellWithOrganelle(organelle => organelle.HasCiliaComponent),
        new AddCellWithOrganelle(organelle => organelle.HasPilusComponent),
        new AddCellWithOrganelle(organelle => organelle.HasAgentVacuoleComponent),
        new AddCellWithOrganelle(organelle => organelle.HasChemoreceptorComponent),
        new AddCellWithOrganelle(organelle => organelle.HasComponentFactory<StorageComponentFactory>()),
        AddCellWithOrganelle.ThatCreateCompound(Compound.Glucose),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellDivisionOfLabourPressure;

    public static CellDivisionOfLabourPressure ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CellDivisionOfLabourPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    /// <summary>
    ///   Scores the body plan of a multicellular species. Single-celled species always get the baseline score as they
    ///   cannot divide labour between cells, which keeps this pressure from pruning them out of the miche tree.
    /// </summary>
    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MulticellularSpecies multicellularSpecies)
        {
            if (species is MicrobeSpecies)
                return BASELINE_SCORE;

            throw new ArgumentException("Wrong type of Species passed to Microbe/Multicellular Species miche tree");
        }

        var composition = TissueComposition.FromMulticellular(multicellularSpecies);

        // A single cell cannot divide labour, so there is nothing to reward here yet
        if (composition.TissueCount < 2)
            return BASELINE_SCORE;

        // Roles only count when they are split between at least two cell types, otherwise a single do-everything
        // cell type would score as if it were a differentiated body plan
        var roleBonus = composition.HasDivisionOfLabour ? composition.RoleCount * ROLE_BONUS : 0;

        var cellTypeBonus = Math.Min(composition.CellTypeCount - 1, MAX_BONUSED_CELL_TYPES) * CELL_TYPE_BONUS;

        // One contiguous block per cell type is the ideal arrangement. Every extra block means the same tissue is
        // scattered around the body, which is penalised but never below the baseline so species are not pruned out
        var extraBlocks = Math.Max(0, composition.TissueBlockCount - composition.CellTypeCount);
        var fragmentationPenalty = Math.Min(extraBlocks * FRAGMENTATION_PENALTY, MAX_FRAGMENTATION_PENALTY);

        return Math.Clamp(BASELINE_SCORE + cellTypeBonus + roleBonus - fragmentationPenalty, BASELINE_SCORE,
            MAX_SCORE);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
