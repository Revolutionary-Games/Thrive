

const int OXYGEN_SEARCH_THRESHHOLD = 8;
const int GLUCOSE_SEARCH_THRESHHOLD = 5;

// microbes_number = {}

////////////////////////////////////////////////////////////////////////////////
// MicrobeAIControllerComponent
//
// Component for identifying and determining AI controlled microbes.
////////////////////////////////////////////////////////////////////////////////

// Enum for state machine
    enum LIFESTATE
        {
        NEUTRAL_STATE,
        GATHERING_STATE,
        FLEEING_STATE,
        PREDATING_STATE,
        PLANTLIKE_STATE
        }

class MicrobeAIControllerComponent : ScriptComponent{

    MicrobeAIControllerComponent(){

        intervalRemaining = reevalutationInterval;
    }

    float movementRadius = 2000;
    // That means they evaluate every 10 seconds or so, correct?
    int reevalutationInterval = 1000;
    int intervalRemaining;
    int boredom = 0;
    int ticksSinceLastToggle = 600;
    double previousStoredCompounds = 0.0f;
    Float3 direction = Float3(0, 0, 0);
    double speciesAggression = -1.0f;
    double speciesFear = -1.0f;
    double speciesActivity = -1.0f;
    double speciesFocus = -1.0f;
    bool hasTargetPosition = false;
    Float3 targetPosition = Float3(0, 0, 0);
    bool hasSearchedCompoundId = false;
    CompoundId searchedCompoundId;
    float previousAngle = 0.0f;
    float compoundDifference=0;
    ObjectID prey = NULL_OBJECT;
    bool preyPegged=false;
    // Prey and predator lists
    array<ObjectID> predatoryMicrobes;
    array<ObjectID> preyMicrobes;


    LIFESTATE lifeState = NEUTRAL_STATE;

}


//! \todo Check if there is a better way than caching a single component for this system
class MicrobeAISystemCached{

    MicrobeAISystemCached(ObjectID entity, MicrobeAIControllerComponent@ first,
        MicrobeComponent@ second, Position@ third
    ) {

        this.entity = entity;
        @this.first = first;
        @this.second = second;
        @this.third = third;
    }

    ObjectID entity = -1;

    MicrobeAIControllerComponent@ first;
    MicrobeComponent@ second;
    Position@ third;
}

////////////////////////////////////////////////////////////////////////////////
// MicrobeAISystem
//
// Updates AI controlled microbes
////////////////////////////////////////////////////////////////////////////////
class MicrobeAISystem : ScriptSystem{

    void Init(GameWorld@ w){

        @this.world = cast<CellStageWorld>(w);
        assert(this.world !is null, "MicrobeAISystem expected CellStageWorld");
    }

    void Release(){}

    void Run(){
        const int logicTime = TICKSPEED;

        // TODO: this could be cached better
        CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");

        // This list is quite expensive to build each frame but
        // there's currently no good way to cache this
        array<ObjectID>@ allMicrobes = world.GetScriptComponentHolder("MicrobeComponent").GetIndex();


        for(uint i = 0; i < CachedComponents.length(); ++i){

            MicrobeAISystemCached@ components = CachedComponents[i];

            ObjectID microbeEntity = components.entity;
            MicrobeAIControllerComponent@ aiComponent = components.first;
            MicrobeComponent@ microbeComponent = components.second;
            Position@ position = components.third;
            // ai interval
            aiComponent.intervalRemaining += logicTime;
            // Cache fear and aggression as we dont wnat to be calling "getSpecies" every frame for every microbe (maybe its not a big deal)
            SpeciesComponent@ ourSpecies = MicrobeOperations::getSpeciesComponent(world, microbeEntity);
            if (ourSpecies !is null){
            if (aiComponent.speciesAggression == -1.0f){
                aiComponent.speciesAggression = ourSpecies.aggression;
                }
            if (aiComponent.speciesFear == -1.0f){
                aiComponent.speciesFear = ourSpecies.fear;
                }
            if (aiComponent.speciesActivity == -1.0f){
                aiComponent.speciesActivity =ourSpecies.activity;
                }
            if (aiComponent.speciesFocus == -1.0f){
                aiComponent.speciesFocus = ourSpecies.focus;
                }
            }
                // Were for debugging
                /*LOG_INFO("AI aggression"+aiComponent.speciesAggression);
                LOG_INFO("AI fear"+aiComponent.speciesFear);
                LOG_INFO("AI Focus"+aiComponent.speciesFocus);
                LOG_INFO("AI Activity"+aiComponent.speciesActivity);*/

            while(aiComponent.intervalRemaining > aiComponent.reevalutationInterval){
                aiComponent.intervalRemaining -= aiComponent.reevalutationInterval;
                int numberOfAgentVacuoles = int(
                    microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

                // Clear the lists
                aiComponent.predatoryMicrobes.removeRange(0,aiComponent.predatoryMicrobes.length());
                aiComponent.preyMicrobes.removeRange(0,aiComponent.preyMicrobes.length());
                ObjectID prey = NULL_OBJECT;
                // Peg your prey
                if (!aiComponent.preyPegged){
                    aiComponent.prey=NULL_OBJECT;
                    prey = getNearestPreyItem(components,allMicrobes);
                    aiComponent.prey = prey;
                    if (prey != NULL_OBJECT){
                        aiComponent.preyPegged=true;
                    }
                }
                ObjectID predator = getNearestPredatorItem(components,allMicrobes);

                //30 seconds about
                if (aiComponent.boredom == GetEngine().GetRandom().GetNumber(aiComponent.speciesFocus,1000.0f+aiComponent.speciesFocus)){
                    // Occassionally you need to reevaluate things
                    aiComponent.boredom = 0;
                    if (rollCheck(aiComponent.speciesActivity, 400)){
                        //LOG_INFO("gather only");
                        aiComponent.lifeState = PLANTLIKE_STATE;
                    }
                    else{
                        aiComponent.lifeState = NEUTRAL_STATE;
                        }
                }
                else{
                    aiComponent.boredom++;
                }

                switch (aiComponent.lifeState)
                    {
                    case PLANTLIKE_STATE:
                        {
                        // This ai would idealy just sit there, until it sees a nice opportunity pop-up unlike neutral, which wanders randomly (has a gather chance) until something interesting pops up
                        break;
                        }
                    case NEUTRAL_STATE:
                        {
                        //In this state you just sit there and analyze your environment
                        aiComponent.boredom=0;
                        aiComponent.preyPegged=false;
                        evaluateEnvironment(components,aiComponent.prey,predator);
                        break;
                        }
                    case GATHERING_STATE:
                        {
                        //In this state you gather compounds
                        doRunAndTumble(components);
                        break;
                        }
                    case FLEEING_STATE:
                        {
                        //In this state you run from predatory microbes
                        if (predator != NULL_OBJECT){
                            //aiComponent.hasTargetPosition = false;
                            dealWithPredators(components,predator);
                            }
                        else{
                            if (rollCheck(aiComponent.speciesActivity, 400)){
                                //LOG_INFO("gather only");
                                aiComponent.lifeState = PLANTLIKE_STATE;
                                aiComponent.boredom=0;
                                }
                            else{
                                aiComponent.lifeState = NEUTRAL_STATE;
                                }
                            }
                        break;
                        }
                    case PREDATING_STATE:
                        {
                        if (aiComponent.preyPegged && aiComponent.prey != NULL_OBJECT){
                            dealWithPrey(components, aiComponent.prey, allMicrobes);
                            }
                        else{
                            if (rollCheck(aiComponent.speciesActivity, 400)){
                                //LOG_INFO("gather only");
                                aiComponent.lifeState = NEUTRAL_STATE;
                                aiComponent.boredom=0;
                                }
                            else{
                                aiComponent.lifeState = NEUTRAL_STATE;
                                }
                            }
                        break;
                        }
                    }

            /* Check if we are willing to run, and there is a predator nearby, if so, flee for your life
               If it was ran in evaluate environment, it would only work if the microbe was in the neutral state.
               So think of this as a "reflex" maybe it should go in its own "doReflex" method,
               because we may need more of these very specific things in the future for things like latching onto rocks */
            // If you are predating and not being engulfed, don't run away until you switch state (keeps predators chasing you even when their predators are nearby)
            // Its not a good survival strategy but it makes the game more fun.
            if (predator != NULL_OBJECT && (aiComponent.lifeState != PREDATING_STATE || microbeComponent.isBeingEngulfed)){
                Float3 testPosition = world.GetComponent_Position(predator)._Position;
                if (rollCheck(aiComponent.speciesFear, 500) || microbeComponent.isBeingEngulfed){
                    MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                        world.GetScriptComponentHolder("MicrobeComponent").Find(predator));
                    if ((position._Position -  testPosition).LengthSquared() <= 500+(secondMicrobeComponent.organelles.length()*4.0f)){
                        if (aiComponent.lifeState != FLEEING_STATE)
                            {
                            // Reset target position for faster fleeing
                            aiComponent.hasTargetPosition = false;
                            }
                        aiComponent.boredom=0;
                        aiComponent.lifeState = FLEEING_STATE;
                    }
                }
            }
            }

            //cache stored compounds for use in the next frame (For Run and tumble)
            aiComponent.compoundDifference = microbeComponent.stored-aiComponent.previousStoredCompounds;
            aiComponent.previousStoredCompounds = microbeComponent.stored;
        }
    }

    // Building the prey list and returning the best option
    ObjectID getNearestPreyItem(MicrobeAISystemCached@ components, array<ObjectID>@ allMicrobes){
        // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        ObjectID chosenPrey = NULL_OBJECT;

        CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
        // Grab the agent amounts so a small cell with a lot of toxins has the courage to attack.
        int numberOfAgentVacuoles = int(
                    microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

        // Retrieve nearest potential prey
        for (uint i = 0; i < allMicrobes.length(); i++){
            // Get the microbe component
            MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                world.GetScriptComponentHolder("MicrobeComponent").Find(allMicrobes[i]));

            // At max aggression add them all
            if (allMicrobes[i] != microbeEntity && (secondMicrobeComponent.speciesName != microbeComponent.speciesName) && !secondMicrobeComponent.dead){
                if ((aiComponent.speciesAggression==MAX_SPECIES_AGRESSION) or
                    ((((numberOfAgentVacuoles+microbeComponent.organelles.length())*1.0f)*(aiComponent.speciesAggression/AGRESSION_DIVISOR)) >
                    (secondMicrobeComponent.organelles.length()*1.0f))){
                    //You are non-threatening to me
                    aiComponent.preyMicrobes.insertLast(allMicrobes[i]);
                    }
            }
            }

            // Get the nearest one if it exists
            if (aiComponent.preyMicrobes.length() > 0 )
            {
            Float3 testPosition = world.GetComponent_Position(aiComponent.preyMicrobes[0])._Position;
            chosenPrey = aiComponent.preyMicrobes[0];
            for (uint c = 0; c < aiComponent.preyMicrobes.length(); c++){
                // Get the microbe component
                MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                    world.GetScriptComponentHolder("MicrobeComponent").Find(aiComponent.preyMicrobes[c]));
                    Position@ thisPosition = world.GetComponent_Position(aiComponent.preyMicrobes[c]);

                    if ((testPosition - position._Position).LengthSquared() > (thisPosition._Position -  position._Position).LengthSquared()){
                        testPosition = thisPosition._Position;
                        chosenPrey = aiComponent.preyMicrobes[c];
                        }
                }
            }
            // It might be interesting to prioritize weakened prey (Maybe add a variable for opportunisticness to each species?)


    return chosenPrey;
    }

    // Building the predator list and retruning the scariest one
    ObjectID getNearestPredatorItem(MicrobeAISystemCached@ components, array<ObjectID>@ allMicrobes){
        // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        ObjectID predator = NULL_OBJECT;

        // Retrive the nearest predator
        // For our desires lets just say all microbes bigger are potential predators
        // and later extend this to include those with toxins and pilus
        for (uint i = 0; i < allMicrobes.length(); i++)
            {
            // Get the microbe component
            MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                world.GetScriptComponentHolder("MicrobeComponent").Find(allMicrobes[i]));
            // Is this an expensive lookup?, ill come up with a more efficient means of doing this.
            CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
            int numberOfAgentVacuoles = int(
                secondMicrobeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);
            // At max fear add them all
            if (allMicrobes[i] != microbeEntity && (secondMicrobeComponent.speciesName != microbeComponent.speciesName) && !secondMicrobeComponent.dead){
            if ((aiComponent.speciesFear==MAX_SPECIES_FEAR) or
            ((((numberOfAgentVacuoles+secondMicrobeComponent.organelles.length())*1.0f)*(aiComponent.speciesFear/FEAR_DIVISOR)) >
            (microbeComponent.organelles.length()*1.0f))){
                //You are bigger then me and i am afraid of that
                aiComponent.predatoryMicrobes.insertLast(allMicrobes[i]);
                //LOG_INFO("Added predator " + allMicrobes[i] );
                }
            }
            }

            // Get the nearest one if it exists
            if (aiComponent.predatoryMicrobes.length() > 0){
            Float3 testPosition = world.GetComponent_Position(aiComponent.predatoryMicrobes[0])._Position;
            predator = aiComponent.predatoryMicrobes[0];

            for (uint c = 0; c < aiComponent.predatoryMicrobes.length(); c++){
                // Get the microbe component
                MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                    world.GetScriptComponentHolder("MicrobeComponent").Find(aiComponent.predatoryMicrobes[c]));
                    Position@ thisPosition = world.GetComponent_Position(aiComponent.predatoryMicrobes[c]);

                    if ((testPosition - position._Position).LengthSquared() > (thisPosition._Position -  position._Position).LengthSquared()){
                        testPosition = thisPosition._Position;
                        predator = aiComponent.predatoryMicrobes[c];
                        }
                }
            }
    return predator;
    }

    // For chasing down and killing prey in various ways
    void dealWithPrey(MicrobeAISystemCached@ components, ObjectID prey, array<ObjectID>@ allMicrobes )
        {
        //LOG_INFO("chasing"+prey);
        // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        // Tick the engulf tick
        aiComponent.ticksSinceLastToggle+=1;
        // Required For AI
        CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");
        CompoundId atpID = SimulationParameters::compoundRegistry().getTypeId("atp");
        //LOG_INFO("predating");
        MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(prey));
        if (secondMicrobeComponent is null){
        aiComponent.preyPegged=false;
        aiComponent.prey = NULL_OBJECT;
        return;
        }
        // Agent vacuoles.
        int numberOfAgentVacuoles = int(
                microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

        // Chase your prey if you dont like acting like a plant
        // Allows for emergence of Predatory Plants (Like a single cleed version of a venus fly trap)
        // Creatures with lethargicness of 400 will not actually chase prey, just lie in wait
        aiComponent.targetPosition =  world.GetComponent_Position(prey)._Position;
        auto vec = (aiComponent.targetPosition - position._Position);
        aiComponent.direction = vec.Normalize();
        microbeComponent.facingTargetPoint = aiComponent.targetPosition;
        aiComponent.hasTargetPosition = true;

        //Always set target Position, for use later in AI
        if (aiComponent.speciesAggression+GetEngine().GetRandom().GetNumber(-100.0f,100.0f) > aiComponent.speciesActivity){
            microbeComponent.movementDirection = Float3(0, 0, -AI_BASE_MOVEMENT);
            }
            else{
            microbeComponent.movementDirection = Float3(0, 0, 0);
            }

            // Turn off engulf if prey is Dead
            // This is probabbly not working
            if (secondMicrobeComponent.dead == true){
                aiComponent.hasTargetPosition = false;
                aiComponent.prey=getNearestPreyItem(components, allMicrobes);
                if (aiComponent.prey != NULL_OBJECT ) {
                    aiComponent.preyPegged=true;
                }

                if (microbeComponent.engulfMode){
                    MicrobeOperations::toggleEngulfMode(world, microbeEntity);
                }
                //  You got a kill, good job
            auto playerSpecies = MicrobeOperations::getSpeciesComponent(world, "Default");
            if (!microbeComponent.isPlayerMicrobe && microbeComponent.speciesName != playerSpecies.name){
                MicrobeOperations::alterSpeciesPopulation(world,microbeEntity,CREATURE_KILL_POPULATION_GAIN);
                }
            }
            else
            {
                //  Turn on engulfmode if close
                if (((position._Position -  aiComponent.targetPosition).LengthSquared() <= 300+(microbeComponent.organelles.length()*3.0f))
                        && (MicrobeOperations::getCompoundAmount(world,microbeEntity,atpID) >=  1.0f)
                    && !microbeComponent.engulfMode &&
                    (float(microbeComponent.organelles.length()) > (
                        ENGULF_HP_RATIO_REQ*secondMicrobeComponent.organelles.length()))){
                    MicrobeOperations::toggleEngulfMode(world, microbeEntity);
                    aiComponent.ticksSinceLastToggle=0;
                    }
                else if (((position._Position -  aiComponent.targetPosition).LengthSquared() >= 500+(microbeComponent.organelles.length()*3.0f))
                        && (microbeComponent.engulfMode && aiComponent.ticksSinceLastToggle >= AI_ENGULF_INTERVAL)){
                    MicrobeOperations::toggleEngulfMode(world, microbeEntity);
                    aiComponent.ticksSinceLastToggle=0;
                    }
            }

          //  Shoot toxins if able
          // There should be AI that prefers shooting over engulfing, etc, not sure how to model
          // that without a million and one variables perhaps its a mix? Maybe a creature with a focus less then a certain amount simply never attacks that way?
          // Maybe a cvreature with a specific focuis, only ever shoots and never engulfs? Maybe their letharcgicness impacts that? I just dont want each enemy to feal the same you know.
          // For now creatures with a focus under 100 will never shoot.
          //LOG_INFO("Our focus is: "+ aiComponent.speciesFocus);

          if (aiComponent.speciesFocus >= 100.0f){
            if (microbeComponent.hitpoints > 0 && numberOfAgentVacuoles > 0 &&
                (position._Position -  aiComponent.targetPosition).LengthSquared() <= aiComponent.speciesFocus*10.0f){
                    if (MicrobeOperations::getCompoundAmount(world,microbeEntity,oxytoxyId) >= MINIMUM_AGENT_EMISSION_AMOUNT){
                        MicrobeOperations::emitAgent(world,microbeEntity, oxytoxyId,10.0f,aiComponent.speciesFocus*10.0f);
                        }
                    }
            }
        }

    // For self defense (not necessarily fleeing)
    void dealWithPredators(MicrobeAISystemCached@ components, ObjectID predator)
    {
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

        if (GetEngine().GetRandom().GetNumber(0,100) <= 10){
            aiComponent.hasTargetPosition = false;
        }

        // Run From Predator
        if (aiComponent.hasTargetPosition == false){
            preyFlee(microbeEntity, aiComponent, microbeComponent, position, predator);
        }
    }

    void preyFlee(ObjectID microbeEntity, MicrobeAIControllerComponent@ aiComponent,
        MicrobeComponent@ microbeComponent, Position@ position, ObjectID predator)
    {
            CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId(
                "oxytoxy");

            // Agent vacuoles.
            int numberOfAgentVacuoles = int(
                microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

            // If focused you can run away more specifically, if not you freak out and scatter
            if (!rollCheck(aiComponent.speciesFocus,500.0f)){
                // Scatter
                auto randAngle = GetEngine().GetRandom().GetFloat(-2*PI, 2*PI);
                auto randDist = GetEngine().GetRandom().GetFloat(200,aiComponent.movementRadius*10);
                aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
                }
            else
                {
                // Run specifically away
                aiComponent.targetPosition =
                    Float3(GetEngine().GetRandom().GetFloat(-1000.0f,1000.0f),1.0,
                        GetEngine().GetRandom().GetFloat(-1000.0f,1000.0f))*
                        world.GetComponent_Position(predator)._Position;
                }

                auto vec = (aiComponent.targetPosition - position._Position);
                aiComponent.direction = vec.Normalize();
                microbeComponent.facingTargetPoint = aiComponent.targetPosition;
                microbeComponent.movementDirection = Float3(0, 0, -(AI_BASE_MOVEMENT));
                aiComponent.hasTargetPosition = true;

           //Freak out and fire toxins everywhere
          if ((aiComponent.speciesAggression > aiComponent.speciesFear) && rollReverseCheck(aiComponent.speciesFocus, 400.0f)){
            if (microbeComponent.hitpoints > 0 && numberOfAgentVacuoles > 0 &&
                (position._Position -  aiComponent.targetPosition).LengthSquared() <= aiComponent.speciesFocus*10.0f){
                    if (MicrobeOperations::getCompoundAmount(world,microbeEntity,oxytoxyId) >= MINIMUM_AGENT_EMISSION_AMOUNT){
                        MicrobeOperations::emitAgent(world,microbeEntity, oxytoxyId,10.0f,aiComponent.speciesFocus*10.0f);
                        }
                    }
          }

    }

    // For for firguring out which state to enter
    void evaluateEnvironment(MicrobeAISystemCached@ components, ObjectID prey, ObjectID predator)
        {
        //LOG_INFO("evaluating");
        MicrobeAIControllerComponent@ aiComponent = components.first;
        Position@ position = components.third;
       if (rollCheck(aiComponent.speciesActivity,500.0f))
            {
            aiComponent.lifeState = PLANTLIKE_STATE;
            aiComponent.boredom = 0;
            }
        else {
        if (prey != NULL_OBJECT && predator != NULL_OBJECT)
            {
            //LOG_INFO("Both");
            if (GetEngine().GetRandom().GetNumber(0.0f,aiComponent.speciesAggression) >
                    GetEngine().GetRandom().GetNumber(0.0f,aiComponent.speciesFear) &&
                        (aiComponent.preyMicrobes.length() > 0)){
                    aiComponent.lifeState = PREDATING_STATE;
                }
            else if (GetEngine().GetRandom().GetNumber(0.0f,aiComponent.speciesAggression) <
                    GetEngine().GetRandom().GetNumber(0.0f,aiComponent.speciesFear)&&
                        (aiComponent.predatoryMicrobes.length() > 0)){
                    //aiComponent.lifeState = PREDATING_STATE;
                    aiComponent.lifeState = FLEEING_STATE;
                }
            else if (aiComponent.speciesAggression == aiComponent.speciesFear &&
                (aiComponent.preyMicrobes.length() > 0)){
                    // Prefer predating (makes game more fun)
                    aiComponent.lifeState  = PREDATING_STATE;
                }
            else if (rollCheck(aiComponent.speciesFocus,500.0f) && GetEngine().GetRandom().GetNumber(0,10) <= 2){
                aiComponent.lifeState = GATHERING_STATE;
            }
            }
        else if (prey != NULL_OBJECT){
            //LOG_INFO("prey only");
            aiComponent.lifeState = PREDATING_STATE;
            }
        else if (predator != NULL_OBJECT){
            //LOG_INFO("predator only");
            aiComponent.lifeState = FLEEING_STATE;
            // I want gathering to trigger more often so i added this here. Because even with predators around you should still graze
            if (rollCheck(aiComponent.speciesFocus,500.0f) && GetEngine().GetRandom().GetNumber(0,10) <= 5){
                    aiComponent.lifeState = GATHERING_STATE;
                }
            }
        // Every 2 intervals or so
        else if (GetEngine().GetRandom().GetNumber(0,10) < 8){
            //LOG_INFO("gather only");
            aiComponent.lifeState = GATHERING_STATE;
            }
        // Every 10 intervals or so
        else if (rollCheck(aiComponent.speciesActivity,400.0f)){
            //LOG_INFO("gather only");
            aiComponent.lifeState = PLANTLIKE_STATE;
            }
        }
        }

    // For doing run and tumble
    void doRunAndTumble(MicrobeAISystemCached@ components){
    // Run and tumble
    // A biased random walk, they turn more if they are picking up less compounds.
    // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm
    // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

        auto randAngle = aiComponent.previousAngle;
        auto randDist = aiComponent.movementRadius;


         float compoundDifference = aiComponent.compoundDifference;

        // Angle should only change if you havent picked up compounds or picked up less compounds
        if (compoundDifference < 0 && GetEngine().GetRandom().GetNumber(0,10) < 5){
            randAngle = aiComponent.previousAngle+GetEngine().GetRandom().GetFloat(0.1f,1.0f);
            aiComponent.previousAngle = randAngle;
            randDist = GetEngine().GetRandom().GetFloat(200.0f,float(aiComponent.movementRadius));
            aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            }

        // If last round you had 0, then have a high likelihood of turning
        if (compoundDifference < AI_COMPOUND_BIAS && GetEngine().GetRandom().GetNumber(0,10) < 9){
            randAngle = aiComponent.previousAngle+GetEngine().GetRandom().GetFloat(1.0f,2.0f);
            aiComponent.previousAngle = randAngle;
            randDist = GetEngine().GetRandom().GetFloat(200.0f,float(aiComponent.movementRadius));
            aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            }

        if (compoundDifference == 0 && GetEngine().GetRandom().GetNumber(0,10) < 9){
            randAngle = aiComponent.previousAngle+GetEngine().GetRandom().GetFloat(1.0f,2.0f);
            aiComponent.previousAngle = randAngle;
            randDist = GetEngine().GetRandom().GetFloat(200.0f,float(aiComponent.movementRadius));
            aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            }

         // If positive last step you gained compounds
         if (compoundDifference > 0  && GetEngine().GetRandom().GetNumber(0,10) < 5){
            // If found food subtract from angle randomly;
            randAngle = aiComponent.previousAngle-GetEngine().GetRandom().GetFloat(0.1f,0.3f);
            aiComponent.previousAngle = randAngle;
            randDist = GetEngine().GetRandom().GetFloat(200.0f,float(aiComponent.movementRadius));
            aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            }

        // Turn more if not in concentration gradient basiclaly (step is .4 if really no mfood, .3 if less food, .1 if in food)
        aiComponent.previousAngle = randAngle;
        auto vec = (aiComponent.targetPosition - position._Position);
        aiComponent.direction = vec.Normalize();
        microbeComponent.facingTargetPoint = aiComponent.targetPosition;
        microbeComponent.movementDirection = Float3(0, 0, -AI_BASE_MOVEMENT);
        aiComponent.hasTargetPosition = true;

    }

    /* Personality checks */
    //There are cases when we want either or, so heres two state rolls
    //TODO: add method for rolling stat versus stat
    bool rollCheck(double ourStat, double dc){
        return (GetEngine().GetRandom().GetNumber(0.0f,dc) <=  ourStat);
    }

    bool rollReverseCheck(double ourStat, double dc){
        return (ourStat >= GetEngine().GetRandom().GetNumber(0.0f,dc));
    }

    void Clear(){
        CachedComponents.resize(0);
    }

    void CreateAndDestroyNodes(){
        // Delegate to helper //
        ScriptSystemNodeHelper(world, @CachedComponents, SystemComponents);
    }

    private array<MicrobeAISystemCached@> CachedComponents;
    private CellStageWorld@ world;

    private array<ScriptSystemUses> SystemComponents = {
        ScriptSystemUses("MicrobeAIControllerComponent"),
        ScriptSystemUses("MicrobeComponent"),
        ScriptSystemUses(Position::TYPE)
    };
}
