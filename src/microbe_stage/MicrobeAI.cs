using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   AI for a single Microbe. This is a separate class to contain all the AI status variables as well as make the
///   Microbe.cs file cleaner as this AI has a lot of code.
/// </summary>
/// <remarks>
///   <para>
///     This is ran in a background thread so no state changing or scene spawning methods on Microbe may be called.
///   </para>
/// </remarks>
public class MicrobeAI
{
    // ReSharper disable once NotAccessedField.Local
    private readonly Compound iron;
    private readonly Compound oxytoxy;

    [JsonProperty]
    private Microbe microbe;

    [JsonProperty]
    private int boredom;

    // ReSharper disable once CollectionNeverQueried.Local
    [JsonIgnore]
    private List<FloatingChunk> chunkList = new List<FloatingChunk>();

    [JsonProperty]
    private bool hasTargetPosition;

    [JsonProperty]
    private bool moveFocused;

    [JsonProperty]
    private float movementRadius = 2000;

    [JsonProperty]
    private bool moveThisHunt = true;

    // All of the game entities stored here are probable places where disposed objects come from
    // so they are ignored for now
    [JsonIgnore]
    private Microbe predator;

    [JsonProperty]
    private float previousAngle;

    [JsonIgnore]
    private FloatingChunk targetChunk;

    [JsonProperty]
    private Vector3 targetPosition = new Vector3(0, 0, 0);

    /// <summary>
    ///   TODO: change to be the elapsed time instead of AI update count
    /// </summary>
    [JsonProperty]
    private float ticksSinceLastToggle = 600;

    public MicrobeAI(Microbe microbe)
    {
        this.microbe = microbe ?? throw new ArgumentException("no microbe given", nameof(microbe));
        oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        iron = SimulationParameters.Instance.GetCompound("iron");
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
        try
        {
            GetNearestPredatorItem(data.AllMicrobes);
            targetChunk = GetNearestChunkItem(data.AllChunks, data.AllMicrobes);
            var possiblePrey = GetNearestPreyItem(data.AllMicrobes);

            if (predator != null &&
                DistanceFromMe(predator.Translation) < (1500.0 * SpeciesFear / Constants.MAX_SPECIES_FEAR))
            {
                PreyFlee(random);
            }
            else if (targetChunk != null &&
                (targetChunk.Translation - microbe.Translation).LengthSquared()
                <= (20000.0 * SpeciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0)
            {
                PursueAndConsumeChunks(targetChunk, random);
            }
            else if (possiblePrey != null)
            {
                PursuePrey(possiblePrey);
            }
            else
            {
                RunAndTumble(random);
            }
        }
        catch
        {
            RunAndTumble(random);
        }

        // Clear the absorbed compounds for run and rumble
        microbe.TotalAbsorbedCompounds.Clear();
    }

    private FloatingChunk GetNearestChunkItem(List<FloatingChunk> allChunks, List<Microbe> allMicrobes)
    {
        FloatingChunk chosenChunk = null;

        Vector3 testPosition = new Vector3(0, 0, 0);
        bool setPosition = true;

        // Retrieve nearest potential chunk
        foreach (var chunk in allChunks)
        {
            if ((SpeciesOpportunism == Constants.MAX_SPECIES_OPPORTUNISM) ||
                ((microbe.EngulfSize * (SpeciesOpportunism / Constants.OPPORTUNISM_DIVISOR)) >
                    chunk.Size))
            {
                chunkList.Add(chunk);
                var thisPosition = chunk.Translation;

                if (setPosition)
                {
                    testPosition = thisPosition;
                    setPosition = false;
                    chosenChunk = chunk;
                }

                if ((testPosition - microbe.Translation).LengthSquared() >
                    (thisPosition - microbe.Translation).LengthSquared())
                {
                    testPosition = thisPosition;
                    chosenChunk = chunk;
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

            if (numRivals > 3)
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
        Microbe chosenPrey = null;

        // Use the agent amounts so a small cell with a lot of toxins has the courage to attack.

        // Retrieve nearest potential prey
        // Max position
        Vector3 testPosition = new Vector3(0, 0, 0);
        bool setPosition = true;

        foreach (var otherMicrobe in allMicrobes)
        {
            if (!otherMicrobe.Dead)
            {
                if (DistanceFromMe(otherMicrobe.Translation) <
                    (2500.0f * SpeciesAggression / Constants.MAX_SPECIES_AGRESSION)
                    && ICanTryToEatMicrobe(otherMicrobe))
                {
                    var thisPosition = otherMicrobe.Translation;

                    if (setPosition)
                    {
                        testPosition = otherMicrobe.Translation;
                        setPosition = false;
                        chosenPrey = otherMicrobe;
                    }

                    if ((testPosition - microbe.Translation).LengthSquared() >
                        (thisPosition - microbe.Translation).LengthSquared())
                    {
                        testPosition = thisPosition;
                        chosenPrey = otherMicrobe;
                    }
                }
            }
        }

        // It might be interesting to prioritize weakened prey (Maybe add a variable for opportunisticness to each
        // species?)
        return chosenPrey;
    }

    /// <summary>
    ///   Building the predator list and setting the scariest one to be predator
    /// </summary>
    /// <param name="allMicrobes">All microbes.</param>
    private void GetNearestPredatorItem(List<Microbe> allMicrobes)
    {
        // Retrieve the nearest predator
        // For our desires lets just say all microbes bigger are potential predators
        // and later extend this to include those with toxins and pilus

        foreach (var otherMicrobe in allMicrobes)
        {
            if (otherMicrobe == microbe)
                continue;

            // Based on species fear, threshold to be afraid ranged from 0.8 to 1.8 microbe size.
            if (otherMicrobe.Species != microbe.Species
                && !otherMicrobe.Dead
                && otherMicrobe.EngulfSize > microbe.EngulfSize
                * (1.8f - (SpeciesFear) / Constants.MAX_SPECIES_FEAR))
            {
                if (predator == null || DistanceFromMe(predator.Translation) >
                    DistanceFromMe(otherMicrobe.Translation))
                {
                    predator = otherMicrobe;
                }
            }
        }
    }

    private void PursueAndConsumeChunks(FloatingChunk chunk, Random random)
    {
        // Tick the engulf tick
        ticksSinceLastToggle += 1;

        // TODO: do something with the chunk compounds
        // ReSharper disable once NotAccessedVariable
        CompoundBag compounds;

        try
        {
            // ReSharper disable once RedundantAssignment
            compounds = chunk.ContainedCompounds;
            targetPosition = chunk.Translation
                + new Vector3(random.NextFloat() * 10.0f - 5.0f, 0.0f, random.NextFloat() * 10.0f - 5.0f);
            microbe.LookAtPoint = targetPosition;
            SetEngulfIfClose();
        }
        catch (ObjectDisposedException)
        {
            // Turn off engulf if chunk is gone
            targetChunk = null;

            hasTargetPosition = false;

            // You got a consumption, good job
            if (!microbe.IsPlayerMicrobe && !microbe.Species.PlayerSpecies)
            {
                microbe.SuccessfulScavenge();
            }

            return;
        }

        hasTargetPosition = true;

        // Always set target Position, for use later in AI
        microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);
    }

    private void PreyFlee(Random random)
    {
        microbe.EngulfMode = false;

        // Run specifically away
        try
        {
            // A lazy algorithm for running away semi-randomly. Microbe picks a random point around
            // itself to flee to, but will repick if that point ends up too close to the attacker.
            if ((predator.Translation - targetPosition).LengthSquared() < 2500.0)
            {
                var fleeSize = 1500.0f;
                targetPosition = new Vector3(random.Next(-fleeSize, fleeSize), 1.0f,
                    random.Next(-fleeSize, fleeSize)) * microbe.Translation;
                microbe.LookAtPoint = targetPosition;
            }

            // If the predator is right on top of the microbe, there's a chance to try and swing with a pilus.
            if (DistanceFromMe(predator.Translation) < 100.0f &&
                RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGRESSION, random))
            {
                MoveWithRandomTurn(2.5f, 3.0f, random);
            }

            // No matter what, I want to make sure I'm moving
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }
        catch (ObjectDisposedException)
        {
            // Our predator is dead
            predator = null;
            return;
        }

        // Freak out and fire toxins everywhere
        if (SpeciesAggression > SpeciesFear && RollReverseCheck(SpeciesFocus, 400.0f, random))
        {
            if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
                (microbe.Translation - targetPosition).LengthSquared() <= SpeciesFocus * 10.0f)
            {
                if (microbe.Compounds.GetCompoundAmount(oxytoxy) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                {
                    microbe.QueueEmitToxin(oxytoxy);
                }
            }
        }
    }

    private void PursuePrey(Microbe target)
    {
        microbe.EngulfMode = target.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ <=
            microbe.EngulfSize && DistanceFromMe(target.Translation) < 50.0f;
        targetPosition = target.Translation;
        microbe.LookAtPoint = targetPosition;
    }

    // For doing run and tumble
    private void RunAndTumble(Random random)
    {
        // Run and tumble
        // A biased random walk, they turn more if they are picking up less compounds.
        // The scientifically accurate algorithm has been flipped to account for the compound
        // deposits being a lot smaller compared to the microbes
        // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

        //If we are still engulfing for some reason, stop
        microbe.EngulfMode = false;

        float compoundDifference = microbe.TotalAbsorbedCompounds.SumValues();

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

    private float SelectRandomTargetPosition(Random random)
    {
        var randAngle = previousAngle + random.Next(0.1f, 1.0f);
        previousAngle = randAngle;
        var randDist = random.Next(200.0f, movementRadius);
        targetPosition = new Vector3(Mathf.Cos(randAngle) * randDist, 0, Mathf.Sin(randAngle) * randDist);
        return randAngle;
    }

    private void SetEngulfIfClose()
    {
        // Turn on engulfmode if close
        if ((microbe.Translation - targetPosition).LengthSquared() <= 100 + (microbe.EngulfSize * 3.0f))
        {
            microbe.EngulfMode = true;
            ticksSinceLastToggle = 0;
        }
    }

    private void MoveWithRandomTurn(float minTurn, float maxTurn, Random random)
    {
        var turn = random.Next(minTurn, maxTurn);
        if (random.Next(2) == 1)
        {
            turn = -turn;
        }

        var randDist = random.Next(2.0f * SpeciesFear, movementRadius);
        targetPosition = microbe.Translation
            + new Vector3((Mathf.Cos(previousAngle + turn) * randDist),
                0,
                (Mathf.Sin(previousAngle + turn) * randDist));
        previousAngle = previousAngle + turn;
        microbe.LookAtPoint = targetPosition;
        hasTargetPosition = true;
        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    private void SetMoveSpeed(float speed)
    {
        microbe.MovementDirection = new Vector3(0, 0, -speed);
    }

    private bool ICanTryToEatMicrobe(Microbe targetMicrobe)
    {
        return (
            (SpeciesAggression == Constants.MAX_SPECIES_AGRESSION)
            || ((microbe.EngulfSize / targetMicrobe.EngulfSize) >= Constants.ENGULF_SIZE_RATIO_REQ)
        );
    }

    private float DistanceFromMe(Vector3 target)
    {
        return (target - microbe.Translation).LengthSquared();
    }

    private static bool RollCheck(float ourStat, float dc, Random random)
    {
        return random.Next(0.0f, dc) <= ourStat;
    }

    private static bool RollReverseCheck(float ourStat, float dc, Random random)
    {
        return ourStat <= random.Next(0.0f, dc);
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }
}
