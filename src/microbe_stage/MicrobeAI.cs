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
/// </remarks>
public class MicrobeAI
{
    private readonly Compound glucose;
    private readonly Compound oxytoxy;
    private readonly Compound ammonia;
    private readonly Compound phosphates;

    [JsonProperty]
    private Microbe microbe;

    // ReSharper disable once CollectionNeverQueried.Local
    [JsonIgnore]
    private List<FloatingChunk> chunkList = new List<FloatingChunk>();

    [JsonProperty]
    private float previousAngle;

    [JsonProperty]
    private Vector3 targetPosition = new Vector3(0, 0, 0);

    [JsonProperty]
    private Microbe focusedPrey = null;

    [JsonProperty]
    private float pursuitThreshold;

    public MicrobeAI(Microbe microbe)
    {
        this.microbe = microbe ?? throw new ArgumentException("no microbe given", nameof(microbe));
        glucose = SimulationParameters.Instance.GetCompound("glucose");
        oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");
    }

    private float SpeciesAggression => microbe.Species.Aggression;
    private float SpeciesFear => microbe.Species.Fear;
    private float SpeciesActivity => microbe.Species.Activity;
    private float SpeciesFocus => microbe.Species.Focus;
    private float SpeciesOpportunism => microbe.Species.Opportunism;

    public void Think(float delta, Random random, MicrobeAICommonData data)
    {
        _ = delta;

        // Clear the lists
        chunkList.Clear();

        Vector3? predator = null;
        try
        {
            predator = GetNearestPredatorItem(data.AllMicrobes)?.Translation;
        } catch (ObjectDisposedException ex)
        {
            // Do nothing; the predator must already be dead
        }

        Vector3? targetChunk = null;
        try
        {
            targetChunk = GetNearestChunkItem(data.AllChunks, data.AllMicrobes, random)?.Translation;
        }
        catch (ObjectDisposedException ex)
        {
            // Do nothing; the chunk must be gone
        }

        Vector3? prey = null;
        bool engulfPrey = false;
        try
        {
            var possiblePrey = GetNearestPreyItem(data.AllMicrobes);
            if (possiblePrey != null)
            {
                engulfPrey = possiblePrey.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ <=
                microbe.EngulfSize && DistanceFromMe(possiblePrey.Translation) < 10.0f * microbe.EngulfSize;
                prey = possiblePrey.Translation;
            }
        }
        catch
        {
            // Do nothing; the prey must have died
        }


        if (microbe.IsBeingEngulfed)
        {
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }
        else if (predator != null &&
            DistanceFromMe((Vector3)predator) < (1500.0 * SpeciesFear / Constants.MAX_SPECIES_FEAR))
        {
            FleeFromPredators(random, (Vector3)predator);
        }
        else if (targetChunk != null)
        {
            PursueAndConsumeChunks((Vector3)targetChunk, random);
        }
        else if (prey != null)
        {
            EngagePrey((Vector3)prey, random, engulfPrey);
        }
        else
        {
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

        // Clear the absorbed compounds for run and rumble
        microbe.TotalAbsorbedCompounds.Clear();
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
                    chunkList.Add(chunk);

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
            var numRivals = 0;
            foreach (var rival in allMicrobes)
            {
                if (rival != microbe)
                {
                    var rivalDistance = (rival.Translation - chosenChunk.Translation).LengthSquared();
                    if (rivalDistance < 500.0f &&
                        rivalDistance < (microbe.Translation - chosenChunk.Translation).LengthSquared())
                    {
                        numRivals++;
                    }
                }
            }

            var rivalThreshold = SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM / 3 ? 1 :
                SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3 ? 3 :
                5;

            // In rare instances, microbes will choose to be much more ambitious
            if (RollCheck(SpeciesFocus, Constants.MAX_SPECIES_FOCUS, random))
            {
                rivalThreshold *= 2;
            }

            if (numRivals > rivalThreshold)
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
        if (focusedPrey != null)
        {
            var distanceToFocusedPrey = DistanceFromMe(focusedPrey.Translation);
            if (!focusedPrey.Dead && distanceToFocusedPrey <
                    (3500.0f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS))
            {
                if (distanceToFocusedPrey < pursuitThreshold)
                {
                    // Keep chasing, but expect to keep getting closer
                    pursuitThreshold *= 0.95f;
                    return focusedPrey;
                }
                else
                {
                    // If prey hasn't gotten closer by now, it's probably too fast, or juking you
                    // Remember who focused prey is, so that you don't fall for this again
                    return null;
                }
            }
            else
            {
                focusedPrey = null;
            }
        }

        Microbe chosenPrey = null;

        foreach (var otherMicrobe in allMicrobes)
        {
            if (!otherMicrobe.Dead)
            {
                if (DistanceFromMe(otherMicrobe.Translation) <
                    (2500.0f * SpeciesAggression / Constants.MAX_SPECIES_AGRESSION)
                    && ICanTryToEatMicrobe(otherMicrobe))
                {
                    if (chosenPrey == null ||
                        (chosenPrey.Translation - microbe.Translation).LengthSquared() >
                        (otherMicrobe.Translation - microbe.Translation).LengthSquared())
                    {
                        chosenPrey = otherMicrobe;
                    }
                }
            }
        }

        focusedPrey = chosenPrey;
        pursuitThreshold = DistanceFromMe(chosenPrey.Translation) * 3.0f;
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
                if (predator == null || DistanceFromMe(predator.Translation) >
                    DistanceFromMe(otherMicrobe.Translation))
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

        // Always set target Position, for use later in AI
        microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);
    }

    private void FleeFromPredators(Random random, Vector3 predatorLocation)
    {
        microbe.EngulfMode = false;

        targetPosition = (2 * predatorLocation - microbe.Translation) + microbe.Translation;
        microbe.LookAtPoint = targetPosition;

        // If the predator is right on top of the microbe, there's a chance to try and swing with a pilus.
        if (DistanceFromMe(predatorLocation) < 100.0f &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGRESSION, random))
        {
            MoveWithRandomTurn(2.5f, 3.0f, random);
        }

        // If prey is confident enough, it will try and launch toxin at the predator
        if (SpeciesAggression > SpeciesFear &&
            DistanceFromMe(predatorLocation) >
            300.0f - (5.0f * SpeciesAggression) + (6.0f * SpeciesFear) &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGRESSION, random))
        {
            LaunchToxin(predatorLocation);
        }

        // No matter what, I want to make sure I'm moving
        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    private void EngagePrey(Vector3 target, Random random, bool engulf)
    {
        microbe.EngulfMode = engulf;
        targetPosition = target;
        microbe.LookAtPoint = targetPosition;
        if (microbe.Compounds.GetCompoundAmount(oxytoxy) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
        {
            LaunchToxin(target);

            if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGRESSION/5, random))
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
        // Run and tumble
        // A biased random walk, they turn more if they are picking up less compounds.
        // The scientifically accurate algorithm has been flipped to account for the compound
        // deposits being a lot smaller compared to the microbes
        // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

        // If we are still engulfing for some reason, stop
        microbe.EngulfMode = false;

        var usefulCompounds = microbe.TotalAbsorbedCompounds.Where(x => microbe.Compounds.IsUseful(x.Key));

        // If this microbe lacks glucose, don't bother with ammonia and phosphorous
        // This algorithm doesn't try to determine if iron and sulfuric acid is usefull to this microbe
        if (microbe.Compounds.GetCompoundAmount(glucose) < 0.5f)
        {
            usefulCompounds = usefulCompounds.Where(x => x.Key != ammonia && x.Key != phosphates);
        }

        float compoundDifference = 0.0f;
        foreach (KeyValuePair<Compound, float> compound in usefulCompounds)
        {
            compoundDifference += compound.Value;
        }

        // If food density is going down, back up and see if there's some more
        if (compoundDifference < 0 && random.Next(0, 10) < 9)
        {
            MoveWithRandomTurn(2.5f, 3.0f, random);
        }

        // If there isn't any food here, it's a good idea to keep moving
        if (compoundDifference == 0 && random.Next(0, 10) < 5)
        {
            MoveWithRandomTurn(0.0f, 0.4f, random);
        }

        // If positive last step you gained compounds, so let's stick around
        if (compoundDifference > 0)
        {
            // There's a decent chance to turn most of the way around
            if (random.Next(0, 10) < 4)
            {
                MoveWithRandomTurn(0.0f, 3.0f, random);
            }

            // There's a chance to stop for a bit and letting nutrients soak in
            // More opportunistic species will do this less.
            if (random.Next(-Constants.MAX_SPECIES_OPPORTUNISM, Constants.MAX_SPECIES_OPPORTUNISM)
                > SpeciesOpportunism)
            {
                SetMoveSpeed(0.0f);
            }
        }
    }

    private void SetEngulfIfClose()
    {
        // Turn on engulfmode if close
        if ((microbe.Translation - targetPosition).LengthSquared() <= microbe.EngulfSize / 3.14f)
        {
            microbe.EngulfMode = true;
        }
        else
        {
            microbe.EngulfMode = false;
        }
    }

    private void LaunchToxin(Vector3 target)
    {
        if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
            (microbe.Translation - target).LengthSquared() <= SpeciesFocus * 10.0f)
        {
            if (microbe.Compounds.GetCompoundAmount(oxytoxy) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
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

    private bool ICanTryToEatMicrobe(Microbe targetMicrobe)
    {
        return targetMicrobe.Species != microbe.Species && ((SpeciesAggression == Constants.MAX_SPECIES_AGRESSION)
            || (microbe.EngulfSize / targetMicrobe.EngulfSize >= Constants.ENGULF_SIZE_RATIO_REQ));
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
