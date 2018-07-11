

const int OXYGEN_SEARCH_THRESHHOLD = 8;
const int GLUCOSE_SEARCH_THRESHHOLD = 5;
const float AI_MOVEMENT_SPEED = 0.5;

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
        PREDATING_STATE
        }

class MicrobeAIControllerComponent : ScriptComponent{

    MicrobeAIControllerComponent(){

        intervalRemaining = reevalutationInterval;
    }

    int movementRadius = 200;
    // That means they evaluate every 10 seconds or so, correct?
    int reevalutationInterval = 1000;
    int intervalRemaining;
    int boredom = 0;
    double previousStoredCompounds = 0.0f;
    Float3 direction = Float3(0, 0, 0);
    double speciesAggression = -1.0f;
    double speciesFear = -1.0f;
    bool hasTargetPosition = false;
    Float3 targetPosition = Float3(0, 0, 0);
    bool hasSearchedCompoundId = false;
    CompoundId searchedCompoundId;
    ObjectID prey = NULL_OBJECT;
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
            if (aiComponent.speciesAggression == -1.0f)
                {
                aiComponent.speciesAggression = MicrobeOperations::getSpeciesComponent(world, microbeEntity).aggression;
                }
            if (aiComponent.speciesFear == -1.0f)
                {
                aiComponent.speciesFear = MicrobeOperations::getSpeciesComponent(world, microbeEntity).fear;
                }
                // Were for debugging
                //LOG_INFO("AI aggression"+aiComponent.speciesAggression);
                //LOG_INFO("AI fear"+aiComponent.speciesFear);

            while(aiComponent.intervalRemaining > aiComponent.reevalutationInterval) {
                aiComponent.intervalRemaining -= aiComponent.reevalutationInterval;
                int numberOfAgentVacuoles = int(
                    microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

                // Clear the lists
                aiComponent.predatoryMicrobes.removeRange(0,aiComponent.predatoryMicrobes.length());
                aiComponent.preyMicrobes.removeRange(0,aiComponent.preyMicrobes.length());

                // Update most feared microbe and most tasty microbe
                prey=NULL_OBJECT;
                predator=NULL_OBJECT;
                prey = getNearestPreyItem(components,allMicrobes);
                predator = getNearestPredatorItem(components,allMicrobes);

                //30 seconds about
                if (aiComponent.boredom == GetEngine().GetRandom().GetNumber(100,1000)){
                    // Occassionally you need to reevaluate things
                    aiComponent.boredom = 0;
                    aiComponent.lifeState = NEUTRAL_STATE;
                }
                else{
                    aiComponent.boredom++;
                }

                switch (aiComponent.lifeState)
                    {
                    case NEUTRAL_STATE:
                        {
                        //In this state you just sit there and analyze your environment
                        aiComponent.boredom=0;
                        evaluateEnvironment(components,prey,predator);
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
                        //In this state you run from preadtory microbes
                        if (predator != NULL_OBJECT)
                            {
                            dealWithPredators(components,predator);
                            }
                        else{
                            aiComponent.lifeState = NEUTRAL_STATE;
                            //LOG_INFO("null predator");
                            }
                        break;
                        }
                    case PREDATING_STATE:
                        {
                        if (prey != NULL_OBJECT)
                            {
                            dealWithPrey(components,prey);
                            }
                        else{
                            aiComponent.lifeState = NEUTRAL_STATE;
                            //LOG_INFO("null prey");
                            }
                        break;
                        }
                    }

                //     if(numberOfAgentVacuoles > 0 || microbeComponent.maxHitpoints > 100){
                //         this.preyCandidates[6] = Entity(PLAYER_NAME, this.gameState.wrapper);
                //         this.preyEntityToIndexMap[Entity(PLAYER_NAME, this.gameState.wrapper).id] = 6;
                //         auto attempts = 0;
                //         while (aiComponent.prey == null or not aiComponent.prey.exists()
                //             or getComponent( aiComponent.prey, MicrobeComponent) == null
                //             or getComponent( aiComponent.prey, MicrobeComponent).dead
                //             or (getComponent(aiComponent.prey, MicrobeComponent).speciesName ==
                //                 microbeComponent.speciesName)
                //             or this.preyEntityToIndexMap[aiComponent.prey.id] == null or
                //             this.preyEscaped == true and attempts < 6 and this.preycount > 10)
                //         {
                //             aiComponent.prey = this.p; //setting the prey
                //             attempts = attempts + 1;
                //             this.preyEscaped = false;
                //         }


            //         if(attempts < 6 and aiComponent.prey !is null and this.predator == null){
            //             //making sure it is not a prey for someone before start hunting
            //             auto preyMicrobeComponent = getComponent(aiComponent.prey,
            //                 MicrobeComponent);
            //             auto preySceneNodeComponent = getComponent(aiComponent.prey,
            //                 OgreSceneNodeComponent);

            //             vec = (preySceneNodeComponent.transform.position -
            //                 position.transform.position);
            //             if(vec.length() > 25){
            //                 this.preyEscaped = true;
            //             }
            //             if(vec.length() < 25 and vec.length() > 10
            // and MicrobeSystem.getCompoundAmount(microbeEntity, oxytoxyId) > MINIMUM_AGENT_EMISSION_AMOUNT
            //                 and microbeComponent.microbetargetdirection < 10){


            //                 MicrobeSystem.emitAgent(microbeEntity,
            //                     CompoundRegistry.getCompoundId("oxytoxy"), 1);
            //             } else if(vec.length() < 10
            //                 and microbeComponent.maxHitpoints > ENGULF_HP_RATIO_REQ * preyMicrobeComponent.maxHitpoints;
            //                 and not microbeComponent.engulfMode){
            //                 MicrobeSystem.toggleEngulfMode(microbeEntity);
            //             } else if(vec.length() > 15  and microbeComponent.engulfMode){
            //                 MicrobeSystem.toggleEngulfMode(microbeEntity);
            //             }

            //             vec.normalise();
            //             aiComponent.direction = vec;
            //             microbeComponent.facingTargetPoint =
            //                 Vector3(preySceneNodeComponent.transform.position.x,
            //                     preySceneNodeComponent.transform.position.y, 0);
            //             microbeComponent.movementDirection = Vector3(0, AI_MOVEMENT_SPEED, 0);
            //         }
            //     } else {
            //         if(MicrobeSystem.getCompoundAmount(microbeEntity,
            //                 CompoundRegistry.getCompoundId("oxygen")) <= OXYGEN_SEARCH_THRESHHOLD)
            //         {
            //             // If we are NOT currenty heading towards an emitter
            //             // emitters were removed a long time ago...
            //         }
            //     }

            //     targetPosition = aiComponent.targetEmitterPosition;
            //     if(aiComponent.targetEmitterPosition !is null and
            //         aiComponent.targetEmitterPosition.z ~= 0){
            //         aiComponent.targetEmitterPosition = null;
            //     }
            // } else if(MicrobeSystem.getCompoundAmount(microbeEntity,
            //         CompoundRegistry.getCompoundId("glucose")) <= GLUCOSE_SEARCH_THRESHHOLD)
            //   {
            //     // If we are NOT currenty heading towards an emitter

            //   }
            }
            //cache stored compounds for use in the next frame (For Runa nd tumble)
            aiComponent.previousStoredCompounds = microbeComponent.stored;
        }
    }

    // Building the prey list and retruning teh best option
    ObjectID getNearestPreyItem(MicrobeAISystemCached@ components, array<ObjectID>@ allMicrobes){
        // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        ObjectID chosenPrey = NULL_OBJECT;

        // Retrieve nearest potential prey
        for (uint i = 0; i < allMicrobes.length(); i++)
            {
            // Get the microbe component
            MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                world.GetScriptComponentHolder("MicrobeComponent").Find(allMicrobes[i]));

            // At max aggression add them all
            if (allMicrobes[i] != microbeEntity && (MicrobeOperations::getSpeciesComponent(world, allMicrobes[i]).name != MicrobeOperations::getSpeciesComponent(world, microbeEntity).name))
            {
                if ((aiComponent.speciesAggression==MAX_SPECIES_AGRESSION) or
                    (((microbeComponent.organelles.length()*1.0f)*(aiComponent.speciesAggression/AGRESSION_DIVISOR)) >
                    (secondMicrobeComponent.organelles.length()*1.0f)))
                    {
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

            for (uint c = 0; c < aiComponent.preyMicrobes.length(); c++)
                {
                // Get the microbe component
                MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                    world.GetScriptComponentHolder("MicrobeComponent").Find(aiComponent.preyMicrobes[c]));
                    Position@ thisPosition = world.GetComponent_Position(aiComponent.preyMicrobes[c]);

                    if ((testPosition - position._Position).LengthSquared() > (thisPosition._Position -  position._Position).LengthSquared())
                        {
                        testPosition = thisPosition._Position;
                        chosenPrey = aiComponent.preyMicrobes[c];
                        }
                }
            }
       // For getting the prey
                //for (m_microbeEntityId,  in pairs (microbes_number)){
                //         // The m_ prefix is used here for some bizarre reason
                //         // m_microbeEntity

                //         MicrobeComponent@ m_microbeComponent = cast<MicrobeComponent>(
                //             world.GetScriptComponentHolder("MicrobeComponent").Find(m_microbeEntity));

                //         auto m_position = world.GetComponent_RenderNode(m_microbeEntity);

                //         if(this.preys !is null){
                //             auto v = (m_position.transform.position -
                //                 position.transform.position);

                //             if(v.length() < 25 and  v.length() ~= 0 ){
                //                 if(microbeComponent.maxHitpoints > 1.5 *
                //                     m_microbeComponent.maxHitpoints)
                //                 {
                //                     this.preys[m_microbeEntityId] = m_microbeEntity;
                //                 }

                //                 if(numberOfAgentVacuoles !is null and numberOfAgentVacuoles ~= 0
                //                     and (m_microbeComponent.specialStorageOrganelles[oxytoxyId] == null
                //                     or m_microbeComponent.specialStorageOrganelles[oxytoxyId] == 0)
                //                     and this.preys[m_microbeEntityId] == null){

                //                     this.preys[m_microbeEntityId] = m_microbeEntity;
                //                 }
                //             } else if(v.length() > 25 or v.length() == 0){
                //                 this.preys[m_microbeEntityId] = null;
                //             }
                //             if(this.preys[m_microbeEntityId] !is null){
                //                 preyMicrobeComponent = getComponent(this.preys[m_microbeEntityId], MicrobeComponent);
                //                 if(preyMicrobeComponent.maxHitpoints <= this.preyMaxHitpoints){
                //                     this.preyMaxHitpoints = preyMicrobeComponent.maxHitpoints;
                //                     this.p = this.preys[m_microbeEntityId];
                //                 }
                //                 this.preycount = this.preycount + 1;
                //             }
                //         }
                //}
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

            // At max fear add them all
            if (allMicrobes[i] != microbeEntity && (MicrobeOperations::getSpeciesComponent(world, allMicrobes[i]).name != MicrobeOperations::getSpeciesComponent(world, microbeEntity).name))
            {
            if ((aiComponent.speciesFear==MAX_SPECIES_FEAR) or (((secondMicrobeComponent.organelles.length()*1.0f)*(aiComponent.speciesFear/FEAR_DIVISOR)) >
            (microbeComponent.organelles.length()*1.0f)))
                {
                //You are bigger then me and i am afraid of that
                aiComponent.predatoryMicrobes.insertLast(allMicrobes[i]);
                //LOG_INFO("Added predator " + allMicrobes[i] );
                }
            }
            }

            // Get the nearest one if it exists
            if (aiComponent.predatoryMicrobes.length() > 0 )
            {

            Float3 testPosition = world.GetComponent_Position(aiComponent.predatoryMicrobes[0])._Position;
            predator = aiComponent.predatoryMicrobes[0];

            for (uint c = 0; c < aiComponent.predatoryMicrobes.length(); c++)
                {
                // Get the microbe component
                MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
                    world.GetScriptComponentHolder("MicrobeComponent").Find(aiComponent.predatoryMicrobes[c]));
                    Position@ thisPosition = world.GetComponent_Position(aiComponent.predatoryMicrobes[c]);

                    if ((testPosition - position._Position).LengthSquared() > (thisPosition._Position -  position._Position).LengthSquared())
                        {
                        testPosition = thisPosition._Position;
                        predator = aiComponent.predatoryMicrobes[c];
                        }
                }
            }
                // For getting the predator
                //     for(predatorEntityId, predatorEntity in pairs (microbes_number)){
                //         auto predatorMicrobeComponent = getComponent(predatorEntity, MicrobeComponent);
                //         auto predatorSceneNodeComponent = getComponent(predatorEntity, OgreSceneNodeComponent);

                //         auto vec = (predatorSceneNodeComponent.transform.position -
                //             position.transform.position);
                //         if(predatorMicrobeComponent.maxHitpoints > microbeComponent.maxHitpoints
                //             * 1.5 and vec.length() < 25)
                //         {
                //             this.predators[predatorEntityId] = predatorEntity;
                //         }
                //         if (predatorMicrobeComponent.specialStorageOrganelles[oxytoxyId] !is null
                //             and predatorMicrobeComponent.specialStorageOrganelles[oxytoxyId] ~= 0
                //             and (numberOfAgentVacuoles == null or numberOfAgentVacuoles == 0) and
                //             vec.length() < 25)
                //         {
                //             this.predators[predatorEntityId] = predatorEntity;
                //         }
                //         if(vec.length() > 25){
                //             this.predators[predatorEntityId] = null;
                //         }
                //         this.predator = this.predators[predatorEntityId];
                //     }
    return predator;
    }

    // For chasing down and killing prey in various ways
    void dealWithPrey(MicrobeAISystemCached@ components, ObjectID prey)
        {
        //LOG_INFO("chasing"+prey);
        // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        // Chase your prey
        aiComponent.targetPosition =  world.GetComponent_Position(prey)._Position;
        auto vec = (aiComponent.targetPosition - position._Position);
        aiComponent.direction = vec.Normalize();
        microbeComponent.facingTargetPoint = aiComponent.targetPosition;
        microbeComponent.movementDirection = Float3(0, 0, -AI_MOVEMENT_SPEED);
        aiComponent.hasTargetPosition = true;

        MicrobeComponent@ secondMicrobeComponent = cast<MicrobeComponent>(
            world.GetScriptComponentHolder("MicrobeComponent").Find(prey));


            // Turn of engulf if prey is Dead
            // This is probabbly not working
            if (secondMicrobeComponent.dead && microbeComponent.engulfMode){
                MicrobeOperations::toggleEngulfMode(world, microbeEntity);
                aiComponent.boredom=1000;
            }

            //  Turn on engulfmode if close
            if ((position._Position -  aiComponent.targetPosition).LengthSquared() <= 20*microbeComponent.organelles.length()+secondMicrobeComponent.organelles.length() && !microbeComponent.engulfMode &&
                (microbeComponent.organelles.length() > ENGULF_HP_RATIO_REQ*secondMicrobeComponent.organelles.length()))
            {
             MicrobeOperations::toggleEngulfMode(world, microbeEntity);
            }
            else if ((position._Position -  aiComponent.targetPosition).LengthSquared() > 20*microbeComponent.organelles.length()+secondMicrobeComponent.organelles.length() && microbeComponent.engulfMode)
            {
             MicrobeOperations::toggleEngulfMode(world, microbeEntity);
            }
        }

    // For self defense (not nessessarily fleeing)
    void dealWithPredators(MicrobeAISystemCached@ components, ObjectID predator)
        {
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;
        if (GetEngine().GetRandom().GetNumber(0,100) <= 10)
            {
            aiComponent.hasTargetPosition = false;
            }
        // Run From Predator
        if (aiComponent.hasTargetPosition == false)
        {
            if (GetEngine().GetRandom().GetNumber(0,100) <= 40)
                {
                // Scatter
                auto randAngle = GetEngine().GetRandom().GetFloat(-2*PI, 2*PI);
                auto randDist = GetEngine().GetRandom().GetFloat(200,aiComponent.movementRadius*10);
                aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
                auto vec = (aiComponent.targetPosition - position._Position);
                aiComponent.direction = vec.Normalize();
                microbeComponent.facingTargetPoint = aiComponent.targetPosition;
                microbeComponent.movementDirection = Float3(0, 0, -AI_MOVEMENT_SPEED);
                aiComponent.hasTargetPosition = true;
                }
            else
                {
                // Run specifically away
                if (GetEngine().GetRandom().GetNumber(0,100) <= 50)
                {
                    if (world.GetComponent_Position(predator)._Position.X >= position._Position.X)
                        {
                        aiComponent.targetPosition =
                            Float3(GetEngine().GetRandom().GetFloat(-10.0f,-100.0f),1.0,1.0)*
                            world.GetComponent_Position(predator)._Position;
                        }
                    else {
                        aiComponent.targetPosition =
                            Float3(GetEngine().GetRandom().GetFloat(20.0f,100.0f),1.0,1.0)*
                            world.GetComponent_Position(predator)._Position;
                        }
                }
                else if (GetEngine().GetRandom().GetNumber(0,100) <= 50)
                {
                    if (world.GetComponent_Position(predator)._Position.Z >= position._Position.Z)
                        {
                        aiComponent.targetPosition =
                        Float3(1.0,1.0,GetEngine().GetRandom().GetFloat(-10.0f,-100.0f))*
                        world.GetComponent_Position(predator)._Position;
                        }
                    else {
                        aiComponent.targetPosition =
                        Float3(1.0,1.0,GetEngine().GetRandom().GetFloat(20.0f,100.0f))*
                        world.GetComponent_Position(predator)._Position;
                        }

                }
                else {
                    aiComponent.targetPosition =
                        Float3(GetEngine().GetRandom().GetFloat(-100.0f,100.0f),1.0,
                        GetEngine().GetRandom().GetFloat(-100.0f,100.0f))*
                        world.GetComponent_Position(predator)._Position;
                }

                auto vec = (aiComponent.targetPosition - position._Position);
                aiComponent.direction = vec.Normalize();
                microbeComponent.facingTargetPoint = aiComponent.targetPosition;
                microbeComponent.movementDirection = Float3(0, 0, -AI_MOVEMENT_SPEED);
                aiComponent.hasTargetPosition = true;
            }
        }
        }

    // For for firguring out which state to enter
    void evaluateEnvironment(MicrobeAISystemCached@ components, ObjectID prey, ObjectID predator)
        {
        //LOG_INFO("evaluating");
        MicrobeAIControllerComponent@ aiComponent = components.first;
        if (prey != NULL_OBJECT && predator != NULL_OBJECT)
            {
            //LOG_INFO("Both");
            if (aiComponent.speciesAggression > aiComponent.speciesFear)
                {
                aiComponent.lifeState = PREDATING_STATE;
                }
            else if (aiComponent.speciesAggression < aiComponent.speciesFear)
                {
                //aiComponent.lifeState = PREDATING_STATE;
                aiComponent.lifeState = FLEEING_STATE;
                }
            else if (aiComponent.speciesAggression == aiComponent.speciesFear)
                {
                // Very rare
                if (GetEngine().GetRandom().GetNumber(0,9) <= 5)
                    {
                    // Prefer predating by 10% (makes game more fun)
                    aiComponent.lifeState  = PREDATING_STATE;
                    }
                    else {
                    aiComponent.lifeState = FLEEING_STATE;
                    }
                }
            }
        else if (prey != NULL_OBJECT)
            {
            //LOG_INFO("prey only");
            aiComponent.lifeState = PREDATING_STATE;
            }
        else if (predator != NULL_OBJECT)
            {
            //LOG_INFO("predator only");
            aiComponent.lifeState = FLEEING_STATE;
            }
        // Every 10 intervals or so
        else if (GetEngine().GetRandom().GetNumber(0,10) == 1)
            {
            //LOG_INFO("gather only");
            aiComponent.lifeState = GATHERING_STATE;
            }
        }

    // For doing run and tumble
    void doRunAndTumble(MicrobeAISystemCached@ components){
    // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

        if (GetEngine().GetRandom().GetNumber(0,100) <= 10)
            {
            aiComponent.hasTargetPosition = false;
            }

        //make AI move randomly for now
        if (aiComponent.hasTargetPosition == false)
            {
            auto randAngle = GetEngine().GetRandom().GetFloat(0, 2*PI);
            auto randDist = GetEngine().GetRandom().GetFloat(10,aiComponent.movementRadius);
            aiComponent.targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            auto vec = (aiComponent.targetPosition - position._Position);
            aiComponent.direction = vec.Normalize();
            microbeComponent.facingTargetPoint = aiComponent.targetPosition;
            microbeComponent.movementDirection = Float3(0, 0, -AI_MOVEMENT_SPEED);
            aiComponent.hasTargetPosition = true;
        }

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

    // This isn't currently possible to store
    // dictionary microbes = {};

    // //counting number of frames so the prey get updated the fittest prey
    // int preycount = 0;
    // //checking if the prey escaped
    // bool preyEscaped = false;

    // the final predator the cell shall run from
    ObjectID predator = NULL_OBJECT;

    // the final prey the cell should hunt
    ObjectID prey = NULL_OBJECT;

}
