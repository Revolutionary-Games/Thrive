using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   AI for a single Microbe. This is a separate class to contain all the AI status variables as well as make the
///   Microbe.cs file cleaner as this AI has a lot of code.
/// </summary>
public class MicrobeAI
{
    private readonly Compound atp;

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
    private LifeState lifeState = LifeState.NEUTRAL_STATE;

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

    // Prey and predator lists
    [JsonIgnore]
    private List<Microbe> predatoryMicrobes = new List<Microbe>();

    [JsonProperty]
    private float previousAngle;

    [JsonIgnore]
    private Microbe prey;

    [JsonIgnore]
    private List<Microbe> preyMicrobes = new List<Microbe>();

    [JsonProperty]
    private bool preyPegged;

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
        atp = SimulationParameters.Instance.GetCompound("atp");
        iron = SimulationParameters.Instance.GetCompound("iron");
    }

    /// <summary>
    ///   Enum for state machine
    /// </summary>
    private enum LifeState
    {
        NEUTRAL_STATE,
        GATHERING_STATE,
        FLEEING_STATE,
        PREDATING_STATE,
        PLANTLIKE_STATE,
        SCAVENGING_STATE,
    }

    private float SpeciesAggression => microbe.Species.Aggression;

    private float SpeciesFear => microbe.Species.Fear;

    private float SpeciesActivity => microbe.Species.Activity;

    private float SpeciesFocus => microbe.Species.Focus;

    private float SpeciesOpportunism => microbe.Species.Opportunism;

    public void Think(float delta, Random random, MicrobeAICommonData data)
    {
        _ = delta;

        // SetRandomTargetAndSpeed(random);

        // Clear the lists
        predatoryMicrobes.Clear();
        preyMicrobes.Clear();
        chunkList.Clear();

        prey = null;

        // 30 seconds about
        if (boredom == (int)random.Next(SpeciesFocus * 2, 1000.0f + SpeciesFocus * 2))
        {
            // Occasionally you need to reevaluate things
            boredom = 0;
            if (RollCheck(SpeciesActivity, 400, random))
            {
                lifeState = LifeState.PLANTLIKE_STATE;
            }
            else
            {
                lifeState = LifeState.NEUTRAL_STATE;
            }
        }
        else
        {
            boredom++;
        }

        switch (lifeState)
        {
            case LifeState.PLANTLIKE_STATE:
                // This ai would ideally just sit there, until it sees a nice opportunity pop-up unlike neutral,
                // which wanders randomly (has a gather chance) until something interesting pops up
                break;
            case LifeState.NEUTRAL_STATE:
            {
                // Before these would run every time, now they just run for the states that need them.
                boredom = 0;
                preyPegged = false;
                prey = null;
                if (predator == null)
                {
                    GetNearestPredatorItem(data.AllMicrobes);
                }

                // Peg your prey
                if (!preyPegged)
                {
                    prey = null;
                    prey = GetNearestPreyItem(data.AllMicrobes);
                    if (prey != null)
                    {
                        preyPegged = true;
                    }
                }

                if (targetChunk == null)
                {
                    targetChunk = GetNearestChunkItem(data.AllChunks);
                }

                EvaluateEnvironment(random);
                break;
            }

            case LifeState.GATHERING_STATE:
            {
                // In this state you gather compounds
                if (RollCheck(SpeciesOpportunism, 400.0f, random))
                {
                    lifeState = LifeState.SCAVENGING_STATE;
                    boredom = 0;
                }
                else
                {
                    DoRunAndTumble(random);
                }

                break;
            }

            case LifeState.FLEEING_STATE:
            {
                if (predator == null)
                {
                    GetNearestPredatorItem(data.AllMicrobes);
                }

                // In this state you run from predatory microbes
                if (predator != null)
                {
                    DealWithPredators(random);
                }
                else
                {
                    if (RollCheck(SpeciesActivity, 400, random))
                    {
                        lifeState = LifeState.PLANTLIKE_STATE;
                        boredom = 0;
                    }
                    else
                    {
                        lifeState = LifeState.NEUTRAL_STATE;
                    }
                }

                break;
            }

            case LifeState.PREDATING_STATE:
            {
                // Peg your prey
                if (!preyPegged)
                {
                    prey = null;
                    prey = GetNearestPreyItem(data.AllMicrobes);
                    if (prey != null)
                    {
                        preyPegged = true;
                    }
                }

                if (preyPegged && prey != null)
                {
                    DealWithPrey(data.AllMicrobes, random);
                }
                else
                {
                    if (RollCheck(SpeciesActivity, 400, random))
                    {
                        lifeState = LifeState.PLANTLIKE_STATE;
                        boredom = 0;
                    }
                    else
                    {
                        lifeState = LifeState.NEUTRAL_STATE;
                    }
                }

                break;
            }

            case LifeState.SCAVENGING_STATE:
            {
                if (targetChunk == null)
                {
                    targetChunk = GetNearestChunkItem(data.AllChunks);
                }

                if (targetChunk != null)
                {
                    DealWithChunks(targetChunk, data.AllChunks);
                }
                else
                {
                    if (!RollCheck(SpeciesOpportunism, 400, random))
                    {
                        lifeState = LifeState.NEUTRAL_STATE;
                        boredom = 0;
                    }
                    else
                    {
                        lifeState = LifeState.SCAVENGING_STATE;
                    }
                }

                break;
            }
        }

        // Run reflexes
        DoReflexes();

        // Clear the absorbed compounds for run and rumble
        microbe.TotalAbsorbedCompounds.Clear();
    }

    /// <summary>
    ///   Clears all the found targets. Currently used for loading from saves
    /// </summary>
    public void ClearAfterLoadedFromSave(Microbe newParent)
    {
        microbe = newParent;
        chunkList?.Clear();
        predator = null;
        predatoryMicrobes.Clear();
        prey = null;
        preyMicrobes.Clear();
        targetChunk = null;

        // Probably should clear this
        preyPegged = false;
    }

    // There are cases when we want either ||, so here's two state rolls
    private static bool RollCheck(float ourStat, float dc, Random random)
    {
        return random.Next(0.0f, dc) <= ourStat;
    }

    private static bool RollReverseCheck(float ourStat, float dc, Random random)
    {
        return ourStat >= random.Next(0.0f, dc);
    }

    private void DoReflexes()
    {
        // For times when its best to tell the microbe directly what to do (Life threatening, attaching to things etc);
        /* Check if we are willing to run, and there is a predator nearby, if so, flee for your life
           If it was ran in evaluate environment, it would only work if the microbe was in the neutral state.
           because we may need more of these very specific things in the future for things like latching onto rocks */
        // If you are predating and not being engulfed, don't run away until you switch state (keeps predators chasing
        // you even when their predators are nearby) Its not a good survival strategy but it makes the game more fun.
        if (predator != null && (lifeState != LifeState.PREDATING_STATE || microbe.IsBeingEngulfed))
        {
            try
            {
                if (!predator.Dead)
                {
                    if ((microbe.Translation - predator.Translation).LengthSquared() <=
                        (2000 + ((predator.HexCount * 8.0f) * 2)))
                    {
                        if (lifeState != LifeState.FLEEING_STATE)
                        {
                            // Reset target position for faster fleeing
                            hasTargetPosition = false;
                        }

                        boredom = 0;
                        lifeState = LifeState.FLEEING_STATE;
                    }
                }
                else
                {
                    predator = null;
                }
            }
            catch (ObjectDisposedException)
            {
                // Our predator might be already disposed
                predator = null;
            }
        }
    }

    /// <summary>
    /// Gets the nearest chunk. And builds a list on chunkList
    /// </summary>
    /// <returns>The nearest chunk item.</returns>
    /// <param name="allChunks">All chunks the AI knows of.</param>
    private FloatingChunk GetNearestChunkItem(List<FloatingChunk> allChunks)
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
            if (otherMicrobe == microbe)
                continue;

            if (otherMicrobe.Species != microbe.Species && !otherMicrobe.Dead)
            {
                if ((SpeciesAggression == Constants.MAX_SPECIES_AGRESSION) ||
                    ((((microbe.AgentVacuoleCount + microbe.EngulfSize) * 1.0f) *
                            (SpeciesAggression / Constants.AGRESSION_DIVISOR)) >
                        (otherMicrobe.EngulfSize * 1.0f)))
                {
                    preyMicrobes.Add(otherMicrobe);

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
        // Retrive the nearest predator
        // For our desires lets just say all microbes bigger are potential predators
        // and later extend this to include those with toxins and pilus
        Vector3 testPosition = new Vector3(0, 0, 0);
        bool setPosition = true;

        foreach (var otherMicrobe in allMicrobes)
        {
            if (otherMicrobe == microbe)
                continue;

            // At max fear add them all
            if (otherMicrobe.Species != microbe.Species && !otherMicrobe.Dead)
            {
                if ((SpeciesFear == Constants.MAX_SPECIES_FEAR) ||
                    ((((microbe.AgentVacuoleCount + otherMicrobe.EngulfSize) * 1.0f) *
                            (SpeciesFear / Constants.FEAR_DIVISOR)) >
                        (microbe.EngulfSize * 1.0f)))
                {
                    // You are bigger then me and i am afraid of that
                    predatoryMicrobes.Add(otherMicrobe);
                    var thisPosition = otherMicrobe.Translation;

                    // At max aggression add them all
                    if (setPosition)
                    {
                        testPosition = thisPosition;
                        setPosition = false;
                        predator = otherMicrobe;
                    }

                    if ((testPosition - microbe.Translation).LengthSquared() >
                        (thisPosition - microbe.Translation).LengthSquared())
                    {
                        testPosition = thisPosition;
                        predator = otherMicrobe;
                    }
                }
            }
        }
    }

    /// <summary>
    /// For chasing down and killing prey in various ways
    /// </summary>
    private void DealWithPrey(List<Microbe> allMicrobes, Random random)
    {
        // Tick the engulf tick
        ticksSinceLastToggle += 1;

        bool lostPrey = false;

        try
        {
            targetPosition = prey.Translation;
        }
        catch (ObjectDisposedException)
        {
            lostPrey = true;
        }

        if (lostPrey)
        {
            preyPegged = false;
            prey = null;
            return;
        }

        // Chase your prey if you dont like acting like a plant
        // Allows for emergence of Predatory Plants (Like a single cleed version of a venus fly trap)
        // Creatures with lethargicness of 400 will not actually chase prey, just lie in wait
        microbe.LookAtPoint = targetPosition;
        hasTargetPosition = true;

        // Always set target Position, for use later in AI
        if (moveThisHunt)
        {
            if (moveFocused)
            {
                microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_FOCUSED_MOVEMENT);
            }
            else
            {
                microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);
            }
        }
        else
        {
            microbe.MovementDirection = new Vector3(0, 0, 0);
        }

        // Turn off engulf if prey is Dead
        // This is probabbly not working. This is almost certainly not working in the Godot version
        if (prey.Dead)
        {
            hasTargetPosition = false;
            prey = GetNearestPreyItem(allMicrobes);
            if (prey != null)
            {
                preyPegged = true;
            }

            microbe.EngulfMode = false;

            // You got a kill, good job
            if (!microbe.IsPlayerMicrobe && !microbe.Species.PlayerSpecies)
            {
                microbe.SuccessfulKill();
            }

            if (RollCheck(SpeciesOpportunism, 400.0f, random))
            {
                lifeState = LifeState.SCAVENGING_STATE;
                boredom = 0;
            }
        }
        else
        {
            // Turn on engulfmode if close
            if ((microbe.Translation - targetPosition).LengthSquared() <= 300 + microbe.EngulfSize * 3.0f
                && microbe.Compounds.GetCompoundAmount(atp) >= 1.0f
                && !microbe.EngulfMode &&
                microbe.EngulfSize > Constants.ENGULF_SIZE_RATIO_REQ * prey.EngulfSize)
            {
                microbe.EngulfMode = true;
                ticksSinceLastToggle = 0;
            }
            else if ((microbe.Translation - targetPosition).LengthSquared() >= 500 + microbe.EngulfSize * 3.0f &&
                microbe.EngulfMode && ticksSinceLastToggle >= Constants.AI_ENGULF_INTERVAL)
            {
                microbe.EngulfMode = false;
                ticksSinceLastToggle = 0;
            }
        }

        // Shoot toxins if able There should be AI that prefers shooting over engulfing, etc, not sure how to model that
        // without a million and one variables perhaps its a mix? Maybe a creature with a focus less then a certain
        // amount simply never attacks that way?  Maybe a creature with a specific focus, only ever shoots and never
        // engulfs? Maybe their lethargicness impacts that? I just dont want each enemy to feel the same you know.  For
        // now creatures with a focus under 100 will never shoot.
        if (SpeciesFocus >= 100.0f)
        {
            if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
                (microbe.Translation - targetPosition).LengthSquared() <= SpeciesFocus * 10.0f)
            {
                if (microbe.Compounds.GetCompoundAmount(oxytoxy) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                {
                    microbe.EmitToxin(oxytoxy);
                }
            }
        }
    }

    /// <summary>
    ///   For chasing down and eating chunks in various ways
    /// </summary>
    /// <param name="chunk">Chunk.</param>
    /// <param name="allChunks">All chunks.</param>
    private void DealWithChunks(FloatingChunk chunk, List<FloatingChunk> allChunks)
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
            targetPosition = chunk.Translation;
        }
        catch (ObjectDisposedException)
        {
            // Turn off engulf if chunk is gone
            targetChunk = null;

            hasTargetPosition = false;
            targetChunk = GetNearestChunkItem(allChunks);
            microbe.EngulfMode = false;

            // You got a consumption, good job
            if (!microbe.IsPlayerMicrobe && !microbe.Species.PlayerSpecies)
            {
                microbe.SuccessfulScavenge();
            }

            return;
        }

        microbe.LookAtPoint = targetPosition;
        hasTargetPosition = true;

        // Always set target Position, for use later in AI
        microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);

        // Turn on engulfmode if close
        if ((microbe.Translation - targetPosition).LengthSquared() <= 300 +
            microbe.EngulfSize * 3.0f
            && microbe.Compounds.GetCompoundAmount(atp) >= 1.0f
            && !microbe.EngulfMode &&
            microbe.EngulfSize > Constants.ENGULF_SIZE_RATIO_REQ * chunk.Size)
        {
            microbe.EngulfMode = true;
            ticksSinceLastToggle = 0;
        }
        else if ((microbe.Translation - targetPosition).LengthSquared() >=
            500 + microbe.EngulfSize * 3.0f && microbe.EngulfMode && ticksSinceLastToggle >=
            Constants.AI_ENGULF_INTERVAL)
        {
            microbe.EngulfMode = false;
            ticksSinceLastToggle = 0;
        }
    }

    // For self defense (not necessarily fleeing)
    private void DealWithPredators(Random random)
    {
        if (random.Next(0, 50) <= 10)
        {
            hasTargetPosition = false;
        }

        // Run From Predator
        if (hasTargetPosition == false)
        {
            // check if predator is legit
            bool hasPredator = false;

            try
            {
                if (predator != null && !predator.Dead)
                    hasPredator = true;
            }
            catch (ObjectDisposedException)
            {
                hasPredator = false;
            }

            if (!hasPredator)
            {
                predator = null;
            }

            PreyFlee(random);
        }
    }

    private void PreyFlee(Random random)
    {
        // If focused you can run away more specifically, if not you freak out and scatter
        if (predator == null || !RollCheck(SpeciesFocus, 500.0f, random))
        {
            // Scatter
            var randAngle = random.Next(-2 * Mathf.Pi, 2 * Mathf.Pi);
            var randDist = random.Next(200.0f, movementRadius * 10.0f);
            targetPosition = new Vector3(Mathf.Cos(randAngle) * randDist, 0, Mathf.Sin(randAngle) * randDist);
        }
        else if (predator != null)
        {
            // Run specifically away
            try
            {
                targetPosition = new Vector3(random.Next(-5000.0f, 5000.0f), 1.0f,
                        random.Next(-5000.0f, 5000.0f)) *
                    predator.Translation;
            }
            catch (ObjectDisposedException)
            {
                // Our predator is dead
                predator = null;
                return;
            }
        }

        // TODO: do something with this
        // var vec = microbe.Translation - targetPosition;
        microbe.LookAtPoint = -targetPosition;
        microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);
        hasTargetPosition = true;

        // Freak out and fire toxins everywhere
        if (SpeciesAggression > SpeciesFear && RollReverseCheck(SpeciesFocus, 400.0f, random))
        {
            if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
                (microbe.Translation - targetPosition).LengthSquared() <= SpeciesFocus * 10.0f)
            {
                if (microbe.Compounds.GetCompoundAmount(oxytoxy) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                {
                    microbe.EmitToxin(oxytoxy);
                }
            }
        }
    }

    // For for figuring out which state to enter
    private void EvaluateEnvironment(Random random)
    {
        if (RollCheck(SpeciesOpportunism, 500.0f, random))
        {
            lifeState = LifeState.SCAVENGING_STATE;
            boredom = 0;
        }
        else
        {
            if (prey != null && predator != null)
            {
                if (random.Next(0.0f, SpeciesAggression) >
                    random.Next(0.0f, SpeciesFear) &&
                    preyMicrobes.Count > 0)
                {
                    moveThisHunt = !RollCheck(SpeciesActivity, 500.0f, random);

                    if (microbe.AgentVacuoleCount > 0)
                    {
                        moveFocused = RollCheck(SpeciesFocus, 500.0f, random);
                    }

                    lifeState = LifeState.PREDATING_STATE;
                }
                else if (random.Next(0.0f, SpeciesAggression) <
                    random.Next(0.0f, SpeciesFear) &&
                    predatoryMicrobes.Count > 0)
                {
                    lifeState = LifeState.FLEEING_STATE;
                }
                else if (SpeciesAggression == SpeciesFear &&
                    preyMicrobes.Count > 0)
                {
                    // Prefer predating (makes game more fun)
                    moveThisHunt = !RollCheck(SpeciesActivity, 500.0f, random);

                    if (microbe.AgentVacuoleCount > 0)
                    {
                        moveFocused = RollCheck(SpeciesFocus, 500.0f, random);
                    }

                    lifeState = LifeState.PREDATING_STATE;
                }
                else if (RollCheck(SpeciesFocus, 500.0f, random) && random.Next(0, 10) <= 2)
                {
                    lifeState = LifeState.GATHERING_STATE;
                }
            }
            else if (prey != null)
            {
                moveThisHunt = !RollCheck(SpeciesActivity, 500.0f, random);

                if (microbe.AgentVacuoleCount > 0)
                {
                    moveFocused = RollCheck(SpeciesFocus, 500.0f, random);
                }

                lifeState = LifeState.PREDATING_STATE;
            }
            else if (predator != null)
            {
                lifeState = LifeState.FLEEING_STATE;

                // I want gathering to trigger more often so i added this here.
                // Because even with predators around you should still graze
                if (RollCheck(SpeciesFocus, 500.0f, random) && random.Next(0, 10) <= 5)
                {
                    lifeState = LifeState.GATHERING_STATE;
                }
            }
            else if (targetChunk != null)
            {
                lifeState = LifeState.SCAVENGING_STATE;
            }
            else if (random.Next(0, 10) < 8)
            {
                // Every 2 intervals || so
                lifeState = LifeState.GATHERING_STATE;
            }
            else if (RollCheck(SpeciesActivity, 400.0f, random))
            {
                // Every 10 intervals || so
                lifeState = LifeState.PLANTLIKE_STATE;
            }
        }
    }

    // For doing run and tumble
    private void DoRunAndTumble(Random random)
    {
        // Run and tumble
        // A biased random walk, they turn more if they are picking up less compounds.
        // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

        var randAngle = previousAngle;
        float randDist;

        float compoundDifference = microbe.TotalAbsorbedCompounds.SumValues();

        // Angle should only change if you havent picked up compounds || picked up less compounds
        if (compoundDifference < 0 && random.Next(0, 10) < 5)
        {
            randAngle = SelectRandomTargetPosition(random);
        }

        // If last round you had 0, then have a high likelihood of turning
        if (compoundDifference < Constants.AI_COMPOUND_BIAS && random.Next(0, 10) < 9)
        {
            randAngle = SelectRandomTargetPosition(random);
        }

        if (compoundDifference == 0 && random.Next(0, 10) < 9)
        {
            randAngle = SelectRandomTargetPosition(random);
        }

        // If positive last step you gained compounds
        if (compoundDifference > 0 && random.Next(0, 10) < 5)
        {
            // If found food subtract from angle randomly;
            randAngle = previousAngle - random.Next(0.1f, 0.3f);
            previousAngle = randAngle;
            randDist = random.Next(200.0f, movementRadius);
            targetPosition = new Vector3(Mathf.Cos(randAngle) * randDist, 0, Mathf.Sin(randAngle) * randDist);
        }

        // Turn more if not in concentration gradient basically (step is .4 if really no food, .3 if less food, .1 if
        // in food)
        previousAngle = randAngle;

        // TODO: do something with this
        // var vec = targetPosition - microbe.Translation;
        microbe.LookAtPoint = targetPosition;
        microbe.MovementDirection = new Vector3(0.0f, 0.0f, -Constants.AI_BASE_MOVEMENT);
        hasTargetPosition = true;
    }

    private float SelectRandomTargetPosition(Random random)
    {
        var randAngle = previousAngle + random.Next(0.1f, 1.0f);
        previousAngle = randAngle;
        var randDist = random.Next(200.0f, movementRadius);
        targetPosition = new Vector3(Mathf.Cos(randAngle) * randDist, 0, Mathf.Sin(randAngle) * randDist);
        return randAngle;
    }

    /// <summary>
    ///   This makes the microbe to do some random movement, used by the AI when nothing else should be done
    /// </summary>
    private void SetRandomTargetAndSpeed(Random random)
    {
        // Set a random nearby look at location
        microbe.LookAtPoint = microbe.Translation + new Vector3(
            random.Next(-200, 201), 0, random.Next(-200, 201));

        // And random movement speed
        microbe.MovementDirection = new Vector3(0, 0, (float)(-1 * random.NextDouble()));
    }
}
