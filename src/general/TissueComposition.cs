using System.Collections.Generic;
using System.Numerics;

/// <summary>
///   Derived, read-only summary of what a species body is made of. It describes the body plan in terms of tissues:
///   how many tissue units there are, how many distinct cell types they use and which functional roles those cell
///   types cover.
/// </summary>
/// <remarks>
///   <para>
///     This is intentionally derived data and is not saved. Tissue level information comes from the existing cell
///     types (multicellular cells and macroscopic metaballs), so every old save keeps loading and every new body plan
///     automatically gets a tissue composition without a save version bump.
///   </para>
///   <para>
///     The composition is the shared skeleton that multicellular and macroscopic code can both build on: scoring,
///     editor UI and future organ systems can ask the same object instead of each deriving roles again.
///   </para>
/// </remarks>
public class TissueComposition
{
    private TissueComposition(int tissueCount, int cellTypeCount, int tissueBlockCount,
        CellFunctionalRole coveredRoles, IReadOnlyDictionary<CellFunctionalRole, int> tissuesPerRole)
    {
        TissueCount = tissueCount;
        CellTypeCount = cellTypeCount;
        TissueBlockCount = tissueBlockCount;
        CoveredRoles = coveredRoles;
        TissuesPerRole = tissuesPerRole;
    }

    /// <summary>
    ///   Number of tissue units in the body: cells for multicellular species, metaballs for macroscopic species.
    /// </summary>
    public int TissueCount { get; }

    /// <summary>
    ///   Number of distinct cell types used by the body plan.
    /// </summary>
    public int CellTypeCount { get; }

    /// <summary>
    ///   Number of contiguous tissue blocks. A block is a connected group of touching tissue units of the same cell
    ///   type, so a body plan with the same cell type in two separate places counts as two blocks.
    /// </summary>
    public int TissueBlockCount { get; }

    /// <summary>
    ///   Combined roles covered by the cell types used in the body plan.
    /// </summary>
    public CellFunctionalRole CoveredRoles { get; }

    /// <summary>
    ///   How many tissue units cover each role (one unit can cover several roles).
    /// </summary>
    public IReadOnlyDictionary<CellFunctionalRole, int> TissuesPerRole { get; }

    /// <summary>
    ///   Number of tissue units covering the movement role.
    /// </summary>
    public int MovementTissueCount => TissuesPerRole[CellFunctionalRole.Movement];

    /// <summary>
    ///   Number of distinct functional roles covered by the body plan.
    /// </summary>
    public int RoleCount => BitOperations.PopCount((uint)CoveredRoles);

    /// <summary>
    ///   True when the body plan is made of at least two cell types, which is the minimum for division of labour.
    /// </summary>
    public bool HasDivisionOfLabour => CellTypeCount >= 2;

    /// <summary>
    ///   Builds the composition of a multicellular species from its gameplay cells.
    /// </summary>
    public static TissueComposition FromMulticellular(MulticellularSpecies species)
    {
        var cells = species.EditorCells;
        var cellTypes = new List<IReadOnlyCellDefinition>(cells.Count);
        var cellTypeList = new List<CellType>(cells.Count);
        var occupiedHexes = new List<HashSet<Hex>>(cells.Count);
        var temporaryStorage = new List<Hex>();

        var layout = species.ModifiableGameplayCells;
        var hexOwners = new Dictionary<Hex, int>();

        foreach (var cell in cells)
        {
            if (cell.Data is not CellTemplate template)
                continue;

            if (template.CellType is not CellType cellType)
                continue;

            cellTypes.Add(cellType);
            cellTypeList.Add(cellType);

            layout.GetHexComponentPositions(template, temporaryStorage);
            var occupied = new HashSet<Hex>();

            foreach (var localHex in temporaryStorage)
                occupied.Add(localHex + cell.Position);

            foreach (var occupiedHex in occupied)
                hexOwners.TryAdd(occupiedHex, occupiedHexes.Count);

            occupiedHexes.Add(occupied);
        }

        return FromCellTypes(cellTypes,
            CountBlocks(cellTypeList, occupiedHexes, hexOwners));
    }

    /// <summary>
    ///   Counts connected blocks of cells that share the same cell type. Cells of the same type that only touch through
    ///   a different cell type are separate blocks.
    /// </summary>
    /// <summary>
    ///   Builds the composition of a macroscopic species from its placed metaballs.
    /// </summary>
    public static TissueComposition FromMacroscopic(MacroscopicSpecies species)
    {
        var cellTypes = new List<IReadOnlyCellDefinition>(species.BodyLayout.Count);
        var metaballs = new List<IReadonlyMacroscopicMetaball>(species.BodyLayout.Count);

        foreach (var metaball in species.BodyLayout)
        {
            cellTypes.Add(metaball.CellType);
            metaballs.Add(metaball);
        }

        return FromCellTypes(cellTypes, CountMacroscopicBlocks(metaballs));
    }

    /// <summary>
    ///   Counts contiguous tissue blocks in a macroscopic body. Metaballs of the same cell type that are connected
    ///   through parent links form one block, separate groups of the same type are separate blocks.
    /// </summary>
    /// <summary>
    ///   Builds a composition from a flat list of tissue units (one entry per cell or metaball).
    /// </summary>
    /// <param name="tissueCellTypes">Cell definition of each tissue unit</param>
    /// <param name="tissueBlockCount">Known number of contiguous blocks, or the unit count when unknown</param>
    public static TissueComposition FromCellTypes(IReadOnlyList<IReadOnlyCellDefinition> tissueCellTypes,
        int tissueBlockCount)
    {
        var distinctTypes = new HashSet<IReadOnlyCellDefinition>(ReferenceEqualityComparer.Instance);
        var roles = CellFunctionalRole.None;
        var tissuesPerRole = new Dictionary<CellFunctionalRole, int>();

        foreach (var cellType in tissueCellTypes)
        {
            var typeRoles = CellFunctionalRoleCalculator.GetTissueRoles(cellType);
            roles |= typeRoles;

            foreach (var role in CellFunctionalRoleCalculator.Split(typeRoles))
            {
                tissuesPerRole.TryGetValue(role, out var current);
                tissuesPerRole[role] = current + 1;
            }

            distinctTypes.Add(cellType);
        }

        foreach (var role in CellFunctionalRoleCalculator.AllRoles)
            tissuesPerRole.TryAdd(role, 0);

        return new TissueComposition(tissueCellTypes.Count, distinctTypes.Count, tissueBlockCount, roles,
            tissuesPerRole);
    }

    public override string ToString()
    {
        return $"Tissues: {TissueCount}, blocks: {TissueBlockCount}, cell types: {CellTypeCount}, " +
            $"roles: {RoleCount}";
    }

    private static int CountBlocks(IReadOnlyList<CellType> cellTypes, IReadOnlyList<HashSet<Hex>> occupiedHexes,
        IReadOnlyDictionary<Hex, int> hexOwners)
    {
        var blocks = 0;
        var visited = new bool[cellTypes.Count];

        for (var i = 0; i < cellTypes.Count; ++i)
        {
            if (visited[i])
                continue;

            ++blocks;
            visited[i] = true;
            var queue = new Queue<int>();
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var hex in occupiedHexes[current])
                {
                    foreach (var offset in Hex.HexNeighbourOffset.Values)
                    {
                        var neighbour = hex + offset;

                        if (!hexOwners.TryGetValue(neighbour, out var neighbourIndex) ||
                            visited[neighbourIndex] || neighbourIndex >= cellTypes.Count)
                        {
                            continue;
                        }

                        if (ReferenceEquals(cellTypes[neighbourIndex], cellTypes[current]))
                        {
                            visited[neighbourIndex] = true;
                            queue.Enqueue(neighbourIndex);
                        }
                    }
                }
            }
        }

        return blocks;
    }

    private static int CountMacroscopicBlocks(IReadOnlyList<IReadonlyMacroscopicMetaball> metaballs)
    {
        var blocks = 0;
        var visited = new bool[metaballs.Count];

        for (var i = 0; i < metaballs.Count; ++i)
        {
            if (visited[i])
                continue;

            ++blocks;
            visited[i] = true;
            var queue = new Queue<int>();
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                for (var other = 0; other < metaballs.Count; ++other)
                {
                    if (visited[other] || !ReferenceEquals(metaballs[other].CellType, metaballs[current].CellType))
                    {
                        continue;
                    }

                    if (!AreConnected(metaballs[current], metaballs[other]))
                        continue;

                    visited[other] = true;
                    queue.Enqueue(other);
                }
            }
        }

        return blocks;
    }

    private static bool AreConnected(IReadonlyMacroscopicMetaball first, IReadonlyMacroscopicMetaball second)
    {
        return ReferenceEquals(first.Parent, second) || ReferenceEquals(second.Parent, first);
    }
}
