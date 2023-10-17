using System.Collections.Generic;
using System.Linq;
using Godot;

public static class MicrobeInternalCalculations
{
    public static Vector3 MaximumSpeedDirection(IEnumerable<OrganelleTemplate> organelles)
    {
        Vector3 maximumMovementDirection = Vector3.Zero;

        var movementOrganelles = organelles.Where(o => o.Definition.HasMovementComponent)
            .ToList();

        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection += GetOrganelleDirection(organelle);
        }

        // After calculating the sum of all organelle directions we subtract the movement components which
        // are symmetric and we choose the one who would benefit the max-speed the most.
        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection = ChooseFromSymmetricFlagella(movementOrganelles,
                organelle, maximumMovementDirection);
        }

        // If the flagella are positioned symmetrically we assume the forward position as default
        if (maximumMovementDirection == Vector3.Zero)
            return Vector3.Forward;

        return maximumMovementDirection;
    }

    public static Vector3 GetOrganelleDirection(OrganelleTemplate organelle)
    {
        return (Hex.AxialToCartesian(new Hex(0, 0)) - Hex.AxialToCartesian(organelle.Position)).Normalized();
    }

    public static float GetTotalNominalCapacity(IEnumerable<OrganelleTemplate> organelles)
    {
        return organelles.Sum(o => GetNominalCapacityForOrganelle(o.Definition, o.Upgrades));
    }

    public static Dictionary<Compound, float> GetTotalSpecificCapacity(ICollection<OrganelleTemplate> organelles)
    {
        var totalNominalCap = 0.0f;

        foreach (var organelle in organelles)
        {
            totalNominalCap += GetNominalCapacityForOrganelle(organelle.Definition, organelle.Upgrades);
        }

        var capacities = new Dictionary<Compound, float>();

        foreach (var organelle in organelles)
        {
            var specificCapacity = GetAdditionalCapacityForOrganelle(organelle.Definition, organelle.Upgrades);

            if (specificCapacity.Compound == null)
                continue;

            if (capacities.TryGetValue(specificCapacity.Compound, out var currentCapacity))
            {
                capacities[specificCapacity.Compound] = currentCapacity + specificCapacity.Capacity;
            }
            else
            {
                capacities.Add(specificCapacity.Compound, specificCapacity.Capacity + totalNominalCap);
            }
        }

        return capacities;
    }

    public static float GetNominalCapacityForOrganelle(OrganelleDefinition definition, OrganelleUpgrades? upgrades)
    {
        if (upgrades?.CustomUpgradeData is StorageComponentUpgrades storage &&
            storage.SpecializedFor != null)
        {
            return 0;
        }

        if (definition.Components.Storage == null)
            return 0;

        return definition.Components.Storage!.Capacity;
    }

    public static (Compound? Compound, float Capacity)
        GetAdditionalCapacityForOrganelle(OrganelleDefinition definition, OrganelleUpgrades? upgrades)
    {
        if (definition.Components.Storage == null)
            return (null, 0);

        if (upgrades?.CustomUpgradeData is StorageComponentUpgrades storage &&
            storage.SpecializedFor != null)
        {
            var specialization = storage.SpecializedFor;
            var capacity = definition.Components.Storage!.Capacity;
            var extraCapacity = capacity * Constants.VACUOLE_SPECIALIZED_MULTIPLIER;
            return (specialization, extraCapacity);
        }

        return (null, 0);
    }

    public static float CalculateCapacity(IEnumerable<OrganelleTemplate> organelles)
    {
        return organelles.Where(
            o => o.Definition.Components.Storage != null).Sum(o => o.Definition.Components.Storage!.Capacity);
    }

    public static float CalculateSpeed(ICollection<OrganelleTemplate> organelles, MembraneType membraneType,
        float membraneRigidity)
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        // For each direction we calculate the organelles contribution to the movement force
        float forwardsDirectionMovementForce = 0;
        float backwardsDirectionMovementForce = 0;
        float leftwardDirectionMovementForce = 0;
        float rightwardDirectionMovementForce = 0;

        // Force factor for each direction
        float forwardDirectionFactor;
        float backwardDirectionFactor;
        float rightDirectionFactor;
        float leftDirectionFactor;

        foreach (var organelle in organelles)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasMovementComponent)
            {
                Vector3 organelleDirection = GetOrganelleDirection(organelle);

                // We decompose the vector of the organelle orientation in 2 vectors, forward and right
                // To get the backward and left is easy because they are the opposite of those former 2
                forwardDirectionFactor = organelleDirection.Dot(Vector3.Forward);
                backwardDirectionFactor = -forwardDirectionFactor;
                rightDirectionFactor = organelleDirection.Dot(Vector3.Right);
                leftDirectionFactor = -rightDirectionFactor;

                float movementConstant = Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement!.Momentum / 100.0f;

                // We get the movement force for every direction as well
                forwardsDirectionMovementForce += MovementForce(movementConstant, forwardDirectionFactor);
                backwardsDirectionMovementForce += MovementForce(movementConstant, backwardDirectionFactor);
                rightwardDirectionMovementForce += MovementForce(movementConstant, rightDirectionFactor);
                leftwardDirectionMovementForce += MovementForce(movementConstant, leftDirectionFactor);
            }
        }

        var maximumMovementDirection = MaximumSpeedDirection(organelles);

        // Maximum-force direction is not normalized so we need to normalize it here
        maximumMovementDirection = maximumMovementDirection.Normalized();

        // Calculate the maximum total force-factors in the maximum-force direction
        forwardDirectionFactor = maximumMovementDirection.Dot(Vector3.Forward);
        backwardDirectionFactor = -forwardDirectionFactor;
        rightDirectionFactor = maximumMovementDirection.Dot(Vector3.Right);
        leftDirectionFactor = -rightDirectionFactor;

        // Add each movement force to the total movement force in the maximum-force direction.
        organelleMovementForce += MovementForce(forwardsDirectionMovementForce, forwardDirectionFactor);
        organelleMovementForce += MovementForce(backwardsDirectionMovementForce, backwardDirectionFactor);
        organelleMovementForce += MovementForce(rightwardDirectionMovementForce, rightDirectionFactor);
        organelleMovementForce += MovementForce(leftwardDirectionMovementForce, leftDirectionFactor);

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (membraneType.MovementFactor - membraneRigidity * Constants.MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }

    /// <summary>
    ///   Calculates the rotation speed for a cell
    /// </summary>
    /// <param name="organelles">The organelles the cell has with their positions for the calculations</param>
    /// <returns>The rotation slerp factor (speed)</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: should this also be affected by the membrane type?
    ///   </para>
    /// </remarks>
    public static float CalculateRotationSpeed(IEnumerable<IPositionedOrganelle> organelles)
    {
        float inertia = 1;

        int ciliaCount = 0;

        // For simplicity we calculate all cilia af if they are at a uniform (max radius) distance from the center
        float radiusSquared = 1;

        // Simple moment of inertia calculation. Note that it is mass multiplied by square of the distance, so we can
        // use the cheaper distance calculations
        foreach (var organelle in organelles)
        {
            var distance = Hex.AxialToCartesian(organelle.Position).LengthSquared();

            if (organelle.Definition.HasCiliaComponent)
            {
                ++ciliaCount;

                if (radiusSquared < distance)
                    radiusSquared = distance;
            }

            // Ignore the center organelle in rotation calculations
            if (distance < MathUtils.EPSILON)
                continue;

            inertia += distance * organelle.Definition.Mass * Constants.CELL_MOMENT_OF_INERTIA_DISTANCE_MULTIPLIER;
        }

        float speed = Constants.CELL_BASE_ROTATION / inertia;

        // Add the extra speed from cilia after we took away some with the rotational inertia calculation
        if (ciliaCount > 0)
        {
            speed += ciliaCount * Mathf.Sqrt(radiusSquared) * Constants.CILIA_RADIUS_FACTOR_MULTIPLIER *
                Constants.CILIA_ROTATION_FACTOR;
        }

        return Mathf.Clamp(speed, Constants.CELL_MIN_ROTATION, Constants.CELL_MAX_ROTATION);
    }

    /// <summary>
    ///   Converts the speed from <see cref="CalculateRotationSpeed"/> to a user displayable form
    /// </summary>
    /// <param name="rawSpeed">The raw speed value</param>
    /// <returns>Converted value to be shown in the GUI</returns>
    public static float RotationSpeedToUserReadableNumber(float rawSpeed)
    {
        return rawSpeed * 500;
    }

    public static float CalculateDigestionSpeed(int enzymeCount)
    {
        var amount = Constants.ENGULF_COMPOUND_ABSORBING_PER_SECOND;
        var buff = amount * Constants.ENZYME_DIGESTION_SPEED_UP_FRACTION * enzymeCount;

        return amount + buff;
    }

    public static float CalculateTotalDigestionSpeed(IEnumerable<OrganelleTemplate> organelles)
    {
        var multiplier = 0;
        foreach (var organelle in organelles)
        {
            if (organelle.Definition.HasComponentFactory<LysosomeComponentFactory>())
                ++multiplier;
        }

        return CalculateDigestionSpeed(multiplier);
    }

    public static float CalculateDigestionEfficiency(int enzymeCount)
    {
        var absorption = Constants.ENGULF_BASE_COMPOUND_ABSORPTION_YIELD;
        var buff = absorption * Constants.ENZYME_DIGESTION_EFFICIENCY_BUFF_FRACTION * enzymeCount;

        return Mathf.Clamp(absorption + buff, 0.0f, Constants.ENZYME_DIGESTION_EFFICIENCY_MAXIMUM);
    }

    /// <summary>
    ///   Returns the efficiency of all enzymes present in the given organelles.
    /// </summary>
    public static Dictionary<Enzyme, float> CalculateDigestionEfficiencies(IEnumerable<OrganelleTemplate> organelles)
    {
        var enzymes = new Dictionary<Enzyme, int>();
        var result = new Dictionary<Enzyme, float>();

        var lipase = SimulationParameters.Instance.GetEnzyme("lipase");

        foreach (var organelle in organelles)
        {
            if (!organelle.Definition.HasComponentFactory<LysosomeComponentFactory>())
                continue;

            var configuration = organelle.Upgrades?.CustomUpgradeData;
            var upgrades = configuration as LysosomeUpgrades;
            var enzyme = upgrades == null ? lipase : upgrades.Enzyme;

            enzymes.TryGetValue(enzyme, out int count);
            enzymes[enzyme] = count + 1;
        }

        result[lipase] = CalculateDigestionEfficiency(0);

        foreach (var enzyme in enzymes)
        {
            result[enzyme.Key] = CalculateDigestionEfficiency(enzyme.Value);
        }

        return result;
    }

    private static float MovementForce(float movementForce, float directionFactor)
    {
        if (directionFactor < 0)
            return 0;

        return movementForce * directionFactor;
    }

    /// <summary>
    ///   Symmetric flagella are a corner case for speed calculations because the sum of all
    ///   directions is kind of broken in their case, so we have to choose which one of the symmetric flagella
    ///   we must discard from the direction calculation
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Here we only discard if the flagella we input is the "bad" one
    ///   </para>
    /// </remarks>
    private static Vector3 ChooseFromSymmetricFlagella(IEnumerable<OrganelleTemplate> organelles,
        OrganelleTemplate testedOrganelle, Vector3 maximumMovementDirection)
    {
        foreach (var organelle in organelles)
        {
            if (organelle != testedOrganelle &&
                organelle.Position + testedOrganelle.Position == new Hex(0, 0))
            {
                var organelleLength = (maximumMovementDirection - GetOrganelleDirection(organelle)).Length();
                var testedOrganelleLength = (maximumMovementDirection -
                    GetOrganelleDirection(testedOrganelle)).Length();

                if (organelleLength > testedOrganelleLength)
                    return maximumMovementDirection;

                return maximumMovementDirection - GetOrganelleDirection(testedOrganelle);
            }
        }

        return maximumMovementDirection;
    }
}
