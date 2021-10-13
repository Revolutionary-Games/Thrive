using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   AI for a single Microbe. This is a separate class to contain all the AI status variables as well as make the
///   Microbe.cs file cleaner as this AI has a lot of code.
/// </summary>
/// <remarks>
///   <para>
///     This is run in a background thread so no state changing or scene spawning methods on Microbe may be called.
///   </para>
///   <para>
///     TODO: this should be updated to have special handling for cell colonies
///   </para>
/// </remarks>
public class MicrobeAI
{
    private readonly Compound glucose;
    private readonly Compound iron;
    private readonly Compound oxytoxy;
    private readonly Compound ammonia;
    private readonly Compound phosphates;

    [JsonProperty]
    private Microbe microbe;

    [JsonProperty]
    private float previousAngle;

    [JsonProperty]
    private Vector3 targetPosition = new Vector3(0, 0, 0);

    [JsonIgnore]
    private EntityReference<Microbe> focusedPrey = new EntityReference<Microbe>();

    [JsonProperty]
    private float pursuitThreshold;

    /// <summary>
    ///   Stores the value of microbe.totalAbsorbedCompound at tick t-1 before it is cleared and updated at tick t.
    ///   Used for compounds gradient computation.
    /// </summary>
    /// <remarks>
    ///   Memory of the previous absorption step is required to compute gradient (which is a variation).
    ///   Values dictionary rather than single value as they will be combined with variable weights.
    /// </remarks>
    [JsonProperty]
    private Dictionary<Compound, float> previouslyAbsorbedCompounds;

    [JsonIgnore]
    private Dictionary<Compound, float> compoundsSearchWeights;

    public MicrobeAI(Microbe microbe)
    {
        this.microbe = microbe ?? throw new ArgumentException("no microbe given", nameof(microbe));

        glucose = SimulationParameters.Instance.GetCompound("glucose");
        iron = SimulationParameters.Instance.GetCompound("iron");
        oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");

        previouslyAbsorbedCompounds = new Dictionary<Compound, float>(microbe.TotalAbsorbedCompounds);
        compoundsSearchWeights = new Dictionary<Compound, float>();
    }

    private float SpeciesAggression => microbe.Species.Aggression;
    private float SpeciesFear => microbe.Species.Fear;
    private float SpeciesActivity => microbe.Species.Activity;
    private float SpeciesFocus => microbe.Species.Focus;
    private float SpeciesOpportunism => microbe.Species.Opportunism;

    public void Think(float delta, Random random, MicrobeAICommonData data)
    {
        _ = delta;

        // Disable most AI in a colony
        if (microbe.ColonyParent != null)
            return;

        ChooseActions(random, data);

        // Store the absorbed compounds for run and rumble
        previouslyAbsorbedCompounds.Clear();
        foreach (var compound in microbe.TotalAbsorbedCompounds)
        {
            previouslyAbsorbedCompounds[compound.Key] = compound.Value;
        }

        // We clear here for update, this is why we stored above!
        microbe.TotalAbsorbedCompounds.Clear();
    }

    /// <summary>
    ///   Resets AI status when this AI controlled microbe is removed from a colony
    /// </summary>
    public void ResetAI()
    {
        previousAngle = 0;
        targetPosition = Vector3.Zero;
        focusedPrey.Value = null;
        pursuitThreshold = 0;
        microbe.MovementDirection = Vector3.Zero;
        microbe.TotalAbsorbedCompounds.Clear();
    }

    private void ChooseActions(Random random, MicrobeAICommonData data)
    {
        if (microbe.IsBeingEngulfed)
        {
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }

        // If nothing is engulfing me right now, see if there's something that might want to hunt me
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/2323
        Vector3? predator = GetNearestPredatorItem(data.AllMicrobes)?.GlobalTransform.origin;
        if (predator.HasValue &&
            DistanceFromMe(predator.Value) < (1500.0 * SpeciesFear / Constants.MAX_SPECIES_FEAR))
        {
            FleeFromPredators(random, predator.Value);
            return;
        }

        // If there are no threats, look for a chunk to eat
        if (!microbe.Species.MembraneType.CellWall)
        {
            Vector3? targetChunk = GetNearestChunkItem(data.AllChunks, data.AllMicrobes, random)?.Translation;
            if (targetChunk.HasValue)
            {
                PursueAndConsumeChunks(targetChunk.Value, random);
                return;
            }
        }

        // If there are no chunks, look for living prey to hunt
        var possiblePrey = GetNearestPreyItem(data.AllMicrobes);
        if (possiblePrey != null)
        {
            bool engulfPrey = !microbe.Species.MembraneType.CellWall &&
                possiblePrey.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ <=
                microbe.EngulfSize && DistanceFromMe(possiblePrey.GlobalTransform.origin) < 10.0f * microbe.EngulfSize;
            Vector3? prey = possiblePrey.GlobalTransform.origin;

            EngagePrey(prey.Value, random, engulfPrey);
            return;
        }

        // Otherwise just wander around and look for compounds
        if (SpeciesActivity > Constants.MAX_SPECIES_ACTIVITY / 10)
        {
            RunAndTumble(random);
        }
        else
        {
            // This organism is sessile, and will not act until the environment changes
            SetMoveSpeed(0.0f);
        }
    }

    private FloatingChunk GetNearestChunkItem(List<FloatingChunk> allChunks, List<Microbe> allMicrobes, Random random)
    {
        FloatingChunk chosenChunk = null;

        // If the microbe cannot absorb, no need for this
        if (microbe.Membrane.Type.CellWall)
        {
            return null;
        }

        // Retrieve nearest potential chunk
        foreach (var chunk in allChunks)
        {
            if (microbe.EngulfSize > chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ
                && (chunk.Translation - microbe.Translation).LengthSquared()
                <= (20000.0 * SpeciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0)
            {
                if (chunk.ContainedCompounds.Compounds.Any(x => microbe.Compounds.IsUseful(x.Key)))
                {
                    if (chosenChunk == null ||
                        (chosenChunk.Translation - microbe.Translation).LengthSquared() >
                        (chunk.Translation - microbe.Translation).LengthSquared())
                    {
                        chosenChunk = chunk;
                    }
                }
            }
        }

        // Don't bother with chunks when there's a lot of microbes to compete with
        if (chosenChunk != null)
        {
            var rivals = 0;
            var distanceToChunk = (microbe.Translation - chosenChunk.Translation).LengthSquared();
            foreach (var rival in allMicrobes)
            {
                if (rival != microbe)
                {
                    var rivalDistance = (rival.GlobalTransform.origin - chosenChunk.Translation).LengthSquared();
                    if (rivalDistance < 500.0f &&
                        rivalDistance < distanceToChunk)
                    {
                        rivals++;
                    }
                }
            }

            int rivalThreshold;
            if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM / 3)
            {
                rivalThreshold = 1;
            }
            else if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3)
            {
                rivalThreshold = 3;
            }
            else
            {
                rivalThreshold = 5;
            }

            // In rare instances, microbes will choose to be much more ambitious
            if (RollCheck(SpeciesFocus, Constants.MAX_SPECIES_FOCUS, random))
            {
                rivalThreshold *= 2;
            }

            if (rivals > rivalThreshold)
            {
                chosenChunk = null;
            }
        }

        return chosenChunk;
    }

    /// <summary>
    ///   Gets the nearest prey item. And builds the prey list
    /// </summary>
    /// <returns>The nearest prey item.</returns>
    /// <param name="allMicrobes">All microbes.</param>
    private Microbe GetNearestPreyItem(List<Microbe> allMicrobes)
    {
        var focused = focusedPrey.Value;
        if (focused != null)
        {
            var distanceToFocusedPrey = DistanceFromMe(focused.GlobalTransform.origin);
            if (!focused.Dead && distanceToFocusedPrey <
                (3500.0f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS))
            {
                if (distanceToFocusedPrey < pursuitThreshold)
                {
                    // Keep chasing, but expect to keep getting closer
                    pursuitThreshold *= 0.95f;
                    return focused;
                }

                // If prey hasn't gotten closer by now, it's probably too fast, or juking you
                // Remember who focused prey is, so that you don't fall for this again
                return null;
            }

            focusedPrey.Value = null;
        }

        Microbe chosenPrey = null;

        foreach (var otherMicrobe in allMicrobes)
        {
            if (!otherMicrobe.Dead)
            {
                if (DistanceFromMe(otherMicrobe.GlobalTransform.origin) <
                    (2500.0f * SpeciesAggression / Constants.MAX_SPECIES_AGGRESSION)
                    && CanTryToEatMicrobe(otherMicrobe))
                {
                    if (chosenPrey == null ||
                        (chosenPrey.GlobalTransform.origin - microbe.Translation).LengthSquared() >
                        (otherMicrobe.GlobalTransform.origin - microbe.Translation).LengthSquared())
                    {
                        chosenPrey = otherMicrobe;
                    }
                }
            }
        }

        focusedPrey.Value = chosenPrey;
        pursuitThreshold = chosenPrey != null ? DistanceFromMe(chosenPrey.GlobalTransform.origin) * 3.0f : 0.0f;
        return chosenPrey;
    }

    /// <summary>
    ///   Building the predator list and setting the scariest one to be predator
    /// </summary>
    /// <param name="allMicrobes">All microbes.</param>
    private Microbe GetNearestPredatorItem(List<Microbe> allMicrobes)
    {
        Microbe predator = null;
        foreach (var otherMicrobe in allMicrobes)
        {
            if (otherMicrobe == microbe)
                continue;

            // Based on species fear, threshold to be afraid ranges from 0.8 to 1.8 microbe size.
            if (otherMicrobe.Species != microbe.Species
                && !otherMicrobe.Dead
                && otherMicrobe.EngulfSize > microbe.EngulfSize
                * (1.8f - SpeciesFear / Constants.MAX_SPECIES_FEAR))
            {
                if (predator == null || DistanceFromMe(predator.GlobalTransform.origin) >
                    DistanceFromMe(otherMicrobe.GlobalTransform.origin))
                {
                    predator = otherMicrobe;
                }
            }
        }

        return predator;
    }

    private void PursueAndConsumeChunks(Vector3 chunk, Random random)
    {
        // This is a slight offset of where the chunk is, to avoid a forward-facing part blocking it
        targetPosition = chunk + new Vector3(0.5f, 0.0f, 0.5f);
        microbe.LookAtPoint = targetPosition;
        SetEngulfIfClose();

        // Just in case something is obstructing chunk engulfing, wiggle a little sometimes
        if (random.NextDouble() < 0.05)
        {
            MoveWithRandomTurn(0.1f, 0.2f, random);
        }

        // If this Microbe is right on top of the chunk, stop instead of spinning
        if (DistanceFromMe(chunk) < Constants.AI_ENGULF_STOP_DISTANCE)
        {
            SetMoveSpeed(0.0f);
        }
        else
        {
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }
    }

    private void FleeFromPredators(Random random, Vector3 predatorLocation)
    {
        microbe.State = Microbe.MicrobeState.Normal;

        targetPosition = (2 * (microbe.Translation - predatorLocation)) + microbe.Translation;

        microbe.LookAtPoint = targetPosition;

        // If the predator is right on top of the microbe, there's a chance to try and swing with a pilus.
        if (DistanceFromMe(predatorLocation) < 100.0f &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
        {
            MoveWithRandomTurn(2.5f, 3.0f, random);
        }

        // If prey is confident enough, it will try and launch toxin at the predator
        if (SpeciesAggression > SpeciesFear &&
            DistanceFromMe(predatorLocation) >
            300.0f - (5.0f * SpeciesAggression) + (6.0f * SpeciesFear) &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
        {
            LaunchToxin(predatorLocation);
        }

        // No matter what, I want to make sure I'm moving
        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    private void EngagePrey(Vector3 target, Random random, bool engulf)
    {
        microbe.State = engulf ? Microbe.MicrobeState.Engulf : Microbe.MicrobeState.Normal;
        targetPosition = target;
        microbe.LookAtPoint = targetPosition;
        if (CanShootToxin())
        {
            LaunchToxin(target);

            if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION / 5, random))
            {
                SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
            }
        }
        else
        {
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }
    }

    // For doing run and tumble
    private void RunAndTumble(Random random)
    {
        // If this microbe is currently stationary, just initialize by moving in a random direction.
        // Used to get newly spawned microbes to move.
        if (microbe.MovementDirection.Length() == 0)
        {
            MoveWithRandomTurn(0, Mathf.Pi, random);
            return;
        }

        // Run and tumble
        // A biased random walk, they turn more if they are picking up less compounds.
        // The scientifically accurate algorithm has been flipped to account for the compound
        // deposits being a lot smaller compared to the microbes
        // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

        // If we are still engulfing for some reason, stop
        microbe.State = Microbe.MicrobeState.Normal;

        ComputeCompoundsSearchWeights();

        float gradientValue = 0.0f;
        foreach (var compoundWeight in compoundsSearchWeights)
        {
            // Note this is about absorbed quantities (which is all microbe has access to) not the ones in the clouds.
            // Gradient computation is therefore cell-centered, and might be different for different cells.
            float compoundDifference = 0.0f;

            microbe.TotalAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedThisStep);
            previouslyAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedPreviousStep);

            compoundDifference += quantityAbsorbedThisStep - quantityAbsorbedPreviousStep;

            compoundDifference *= compoundWeight.Value;
            gradientValue += compoundDifference;
        }

        // Implement a detection threshold to possibly rule out too tiny variations
        // TODO: possibly include cell capacity correction
        float differenceDetectionThreshold = Constants.AI_GRADIENT_DETECTION_THRESHOLD;

        // If food density is going down, back up and see if there's some more
        if (gradientValue < -differenceDetectionThreshold && random.Next(0, 10) < 9)
        {
            MoveWithRandomTurn(2.5f, 3.0f, random);
        }

        // If there isn't any food here, it's a good idea to keep moving
        if (Math.Abs(gradientValue) <= differenceDetectionThreshold && random.Next(0, 10) < 5)
        {
            MoveWithRandomTurn(0.0f, 0.4f, random);
        }

        // If positive last step you gained compounds, so let's move toward the source
        if (gradientValue > differenceDetectionThreshold)
        {
            // There's a decent chance to turn by 90° to explore gradient
            // 180° is useless since previous position let you absorb less compounds already
            if (random.Next(0, 10) < 4)
            {
                MoveWithRandomTurn(0.0f, 1.5f, random);
            }
        }
    }

    /// <summary>
    ///   Prioritizing compounds that are stored in lesser quantities.
    ///   If ATP-producing compounds are low (less than half storage capacities),
    ///   non ATP-related compounds are discarded.
    ///   Updates compoundsSearchWeights instance dictionary.
    /// </summary>
    private void ComputeCompoundsSearchWeights()
    {
        IEnumerable<Compound> usefulCompounds = microbe.Compounds.Compounds.Keys;

        // If this microbe lacks vital compounds don't bother with ammonia and phosphate
        if (usefulCompounds.Any(
            compound => IsVitalCompound(compound) &&
                microbe.Compounds.GetCompoundAmount(compound) < 0.5f * microbe.Compounds.Capacity))
        {
            usefulCompounds = usefulCompounds.Where(x => x != ammonia && x != phosphates);
        }

        compoundsSearchWeights.Clear();
        foreach (var compound in usefulCompounds)
        {
            // The priority of a compound is inversely proportional to its availability
            // Should be tweaked with consumption
            var compoundPriority = 1 - microbe.Compounds.GetCompoundAmount(compound) / microbe.Compounds.Capacity;

            compoundsSearchWeights.Add(compound, compoundPriority);
        }
    }

    /// <summary>
    ///   Tells if a compound is vital to this microbe.
    ///   Vital compounds are *direct* ATP producers
    ///   TODO: what is used here is a shortcut linked to the current game state:
    ///     such compounds could be used for other processes in future versions
    /// </summary>
    private bool IsVitalCompound(Compound compound)
    {
        return microbe.Compounds.IsUseful(compound) &&
            (compound == glucose || compound == iron);
    }

    private void SetEngulfIfClose()
    {
        // Turn on engulf mode if close
        // Sometimes "close" is hard to discern since microbes can range from straight lines to circles
        if ((microbe.Translation - targetPosition).LengthSquared() <= microbe.EngulfSize * 2.0f)
        {
            microbe.State = Microbe.MicrobeState.Engulf;
        }
        else
        {
            microbe.State = Microbe.MicrobeState.Normal;
        }
    }

    private void LaunchToxin(Vector3 target)
    {
        if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
            (microbe.Translation - target).LengthSquared() <= SpeciesFocus * 10.0f)
        {
            if (CanShootToxin())
            {
                microbe.LookAtPoint = target;
                microbe.QueueEmitToxin(oxytoxy);
            }
        }
    }

    private void MoveWithRandomTurn(float minTurn, float maxTurn, Random random)
    {
        var turn = random.Next(minTurn, maxTurn);
        if (random.Next(2) == 1)
        {
            turn = -turn;
        }

        var randDist = random.Next(SpeciesActivity, Constants.MAX_SPECIES_ACTIVITY);
        targetPosition = microbe.Translation
            + new Vector3(Mathf.Cos(previousAngle + turn) * randDist,
                0,
                Mathf.Sin(previousAngle + turn) * randDist);
        previousAngle = previousAngle + turn;
        microbe.LookAtPoint = targetPosition;
        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    private void SetMoveSpeed(float speed)
    {
        microbe.MovementDirection = new Vector3(0, 0, -speed);
    }

    private bool CanTryToEatMicrobe(Microbe targetMicrobe)
    {
        var sizeRatio = microbe.EngulfSize / targetMicrobe.EngulfSize;

        return targetMicrobe.Species != microbe.Species && (
            (SpeciesOpportunism > Constants.MAX_SPECIES_OPPORTUNISM * 0.3f && CanShootToxin())
            || (sizeRatio >= Constants.ENGULF_SIZE_RATIO_REQ));
    }

    private bool CanShootToxin()
    {
        return microbe.Compounds.GetCompoundAmount(oxytoxy) >=
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * SpeciesFocus / Constants.MAX_SPECIES_FOCUS;
    }

    private float DistanceFromMe(Vector3 target)
    {
        return (target - microbe.Translation).LengthSquared();
    }

    private bool RollCheck(float ourStat, float dc, Random random)
    {
        return random.Next(0.0f, dc) <= ourStat;
    }

    private bool RollReverseCheck(float ourStat, float dc, Random random)
    {
        return ourStat <= random.Next(0.0f, dc);
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }
}
