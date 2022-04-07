using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Early multicellular functionality of the microbes. This is a separate file to put all of the core logic of
///   the overridden behaviours in the same place.
/// </summary>
public partial class Microbe
{
    [JsonProperty]
    private int nextBodyPlanCellToGrowIndex = -1;

    [JsonProperty]
    private bool enoughResourcesForBudding;

    [JsonProperty]
    private Dictionary<Compound, float>? compoundsNeededForNextCell;

    [JsonProperty]
    private Dictionary<Compound, float>? compoundsUsedForMulticellularGrowth;

    [JsonProperty]
    private Dictionary<Compound, float>? totalNeededForMulticellularGrowth;

    [JsonIgnore]
    public bool IsFullyGrownMulticellular => nextBodyPlanCellToGrowIndex >= CastedMulticellularSpecies.Cells.Count;

    public void ApplyMulticellularNonFirstCellSpecies(EarlyMulticellularSpecies species, CellType cellType)
    {
        cachedMicrobeSpecies = null;
        cachedMulticellularSpecies = species;
        MulticellularCellType = cellType;

        Species = species;

        FinishSpeciesSetup();

        // We have to force our membrane to be setup here so that the attach logic will have valid membrane data
        // to work with
        SendOrganellePositionsToMembrane();
    }

    /// <summary>
    ///   Adds the next cell missing from this multicellular species' body plan to this microbe's colony
    /// </summary>
    public void AddMulticellularGrowthCell()
    {
        if (Colony == null)
        {
            MicrobeColony.CreateColonyForMicrobe(this);

            if (Colony == null)
                throw new Exception("An issue occured during colony creation!");

            GD.Print("Created a new colony for multicellular cell");
        }

        var template = CastedMulticellularSpecies.Cells[nextBodyPlanCellToGrowIndex];

        var cell = CreateMulticellularColonyMemberCell(template.CellType);

        // We don't reset our state here in case we want to be in engulf mode

        cell.State = State;

        // Attach the created cell to the right spot in our colony
        var ourTransform = GlobalTransform;

        // TODO: figure out the actually right math for the cell position attach
        // If we don't adjust like this the cells overlap way too much
        var attachVector = ourTransform.origin + ourTransform.basis.Xform(Hex.AxialToCartesian(template.Position));

        // Ensure no tiny y component exists here
        attachVector.y = 0;

        var newCellTransform = new Transform(
            MathUtils.CreateRotationForOrganelle(template.Orientation) * ourTransform.basis.Quat(),
            attachVector);
        cell.GlobalTransform = newCellTransform;

        var newCellPosition = newCellTransform.origin;

        // Adding a cell to a colony snaps it close to its colony parent so we need to find the closes existing
        // cell in the colony to use as that here
        var parent = this;
        var currentDistanceSquared = (newCellPosition - ourTransform.origin).LengthSquared();

        foreach (var colonyMember in Colony.ColonyMembers)
        {
            if (colonyMember == this)
                continue;

            var distance = (colonyMember.GlobalTransform.origin - newCellPosition).LengthSquared();

            if (distance < currentDistanceSquared)
            {
                parent = colonyMember;
                currentDistanceSquared = distance;
            }
        }

        Colony.AddToColony(cell, parent);

        ++nextBodyPlanCellToGrowIndex;
        compoundsNeededForNextCell = null;
    }

    private void HandleMulticellularReproduction()
    {
        if (compoundsNeededForNextCell == null)
        {
            // Need to setup the next cell to be grown in our body plan
            if (IsFullyGrownMulticellular)
            {
                // We have completed our body plan and can (once enough resources) reproduce
                if (enoughResourcesForBudding)
                {
                    allOrganellesDivided = true;
                    ReadyToReproduce();
                }
                else
                {
                    compoundsNeededForNextCell = CastedMulticellularSpecies.Cells[0].CellType
                        .CalculateTotalComposition();
                }

                return;
            }

            compoundsNeededForNextCell = CastedMulticellularSpecies.Cells[nextBodyPlanCellToGrowIndex].CellType
                .CalculateTotalComposition();
        }

        bool stillNeedsSomething = false;

        compoundsUsedForMulticellularGrowth ??= new Dictionary<Compound, float>();

        // Consume some compounds for the next cell in the layout
        // Similar logic for "growing" more cells than in PlacedOrganelle growth
        foreach (var entry in compoundsNeededForNextCell)
        {
            var amountNeeded = entry.Value;

            stillNeedsSomething = true;

            var amountAvailable = Compounds.GetCompoundAmount(entry.Key) -
                Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if (amountAvailable <= MathUtils.EPSILON)
                continue;

            // We can take some
            var amountToTake = Mathf.Min(amountNeeded, amountAvailable);

            var amount = Compounds.TakeCompound(entry.Key, amountToTake);
            var left = amountNeeded - amount;

            if (left < 0.0001f)
            {
                compoundsNeededForNextCell.Remove(entry.Key);
            }
            else
            {
                compoundsNeededForNextCell[entry.Key] = left;
            }

            compoundsUsedForMulticellularGrowth.TryGetValue(entry.Key, out float alreadyUsed);

            compoundsUsedForMulticellularGrowth[entry.Key] = alreadyUsed + amount;

            // As we modify the list, we are content just consuming one type of compound per frame
            break;
        }

        if (!stillNeedsSomething)
        {
            // The current cell to grow is now ready to be added
            // Except in the case that we were just getting resources for budding, skip in that case
            if (!IsFullyGrownMulticellular)
            {
                AddMulticellularGrowthCell();
            }
            else
            {
                // Has collected enough resources to spawn the first cell type as budding type reproduction
                enoughResourcesForBudding = true;
                compoundsNeededForNextCell = null;
            }
        }
    }

    private void ResetMulticellularProgress()
    {
        // Clear variables

        // The first cell is the last to duplicate (budding reproduction) so the body plan starts filling at index 1
        nextBodyPlanCellToGrowIndex = 1;
        enoughResourcesForBudding = false;

        compoundsNeededForNextCell = null;
        compoundsUsedForMulticellularGrowth = null;

        totalNeededForMulticellularGrowth = null;

        // Delete the cells in our colony currently
        if (Colony != null)
        {
            GD.Print("Resetting growth in a multicellular colony");
            var cellsToDestroy = Colony.ColonyMembers.Where(m => m != this).ToList();

            Colony.RemoveFromColony(this);

            foreach (var microbe in cellsToDestroy)
            {
                microbe.DetachAndQueueFree();
            }
        }
    }

    private Microbe CreateMulticellularColonyMemberCell(CellType cellType)
    {
        var newCell = SpawnHelpers.SpawnMicrobe(Species, Translation,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem!, CurrentGame, cellType);

        // Make it despawn like normal (if our colony is accidentally somehow disbanded)
        SpawnSystem.AddEntityToTrack(newCell);

        // Remove the compounds from the created cell
        newCell.Compounds.ClearCompounds();

        // TODO: different sound effect?
        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return newCell;
    }

    private Dictionary<Compound, float> CalculateTotalBodyPlanCompounds()
    {
        if (totalNeededForMulticellularGrowth == null)
        {
            totalNeededForMulticellularGrowth = new Dictionary<Compound, float>();

            foreach (var cell in CastedMulticellularSpecies.Cells)
            {
                totalNeededForMulticellularGrowth.Merge(cell.CellType.CalculateTotalComposition());
            }
        }

        return totalNeededForMulticellularGrowth;
    }
}
