

const int OXYGEN_SEARCH_THRESHHOLD = 8;
const int GLUCOSE_SEARCH_THRESHHOLD = 5;
const float AI_MOVEMENT_SPEED = 0.5;

// microbes_number = {}

////////////////////////////////////////////////////////////////////////////////
// MicrobeAIControllerComponent
//
// Component for identifying and determining AI controlled microbes.
////////////////////////////////////////////////////////////////////////////////
class MicrobeAIControllerComponent : ScriptComponent{

    MicrobeAIControllerComponent(){

        intervalRemaining = reevalutationInterval;
    }

    int movementRadius = 20;
    int reevalutationInterval = 1000;
    int intervalRemaining;
    Float3 direction = Float3(0, 0, 0);
    bool hasTargetEmitterPosition = false;
    Float3 targetEmitterPosition = Float3(0, 0, 0);
    bool hasSearchedCompoundId = false;
    CompoundId searchedCompoundId;
    ObjectID prey = NULL_OBJECT;
}

// void MicrobeAIControllerComponent.storage(storage){

//     storage.set("movementRadius", this.movementRadius);
//     storage.set("reevalutationInterval", this.reevalutationInterval);
//     storage.set("intervalRemaining", this.intervalRemaining);
//     storage.set("direction", this.direction);
//     if(this.targetEmitterPosition == null){
//         storage.set("targetEmitterPosition", "null");
//         else;
//         storage.set("targetEmitterPosition", this.targetEmitterPosition);
//     }
//     if(this.searchedCompoundId == null){
//         storage.set("searchedCompoundId", "null");
//         else;
//         storage.set("searchedCompoundId", this.searchedCompoundId);
//     }

// }

// void MicrobeAIControllerComponent.load(storage){

//     this.movementRadius = storage.get("movementRadius", 20);
//     this.reevalutationInterval = storage.get("reevalutationInterval", 1000);
//     this.intervalRemaining = storage.get("intervalRemaining", this.reevalutationInterval);
//     this.direction = storage.get("direction", Vector3(0, 0, 0));
//     auto emitterPosition = storage.get("targetEmitterPosition", null);
//     if(emitterPosition == "null"){
//         this.targetEmitterPosition = null;
//         else;
//         this.targetEmitterPosition = emitterPosition;
//     }
//     this.searchedCompoundId = storage.get("searchedCompoundId", null);
//     if(this.searchedCompoundId == "null"){
//         this.searchedCompoundId = null;
//     }
// }

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
        // for(_, entityId in pairs(this.entities.removedEntities())){
        //     this.microbes[entityId] = null;
        //     if(this.preyEntityToIndexMap[entityId]){
        //         this.preyCandidates[this.preyEntityToIndexMap[entityId]] = null;
        //         this.preyEntityToIndexMap[entityId] = null;
        //     }
        // }
        // for(_, entityId in pairs(this.entities.addedEntities())){
        //     auto microbeEntity = Entity(entityId, this.gameState.wrapper);
        //     this.microbes[entityId] = microbeEntity;

        //     // This is a hack to remember up to 5 recent microbes as candidates for predators.
        //     // Gives something semi random
        //     this.preyCandidates[this.currentPreyIndex] = microbeEntity;
        //     this.preyEntityToIndexMap[entityId] = this.currentPreyIndex;
        //     this.currentPreyIndex = (this.currentPreyIndex)%6;

        // }

        // //for removing cell from table when it is removed from the world
        // for(_, entityId in pairs(this.microbeEntities.removedEntities())){
        //     microbes_number[entityId] = null;
        // }

        // //for counting all the cells in the world and get it's entity
        // for(_, entityId in pairs(this.microbeEntities.addedEntities())){
        //     auto microbeEntity = Entity(entityId, this.gameState.wrapper);
        //     microbes_number[entityId] = microbeEntity;
        // }

        // this.entities.clearChanges();
        // this.microbeEntities.clearChanges();

        const int logicTime = TICKSPEED;

        // TODO: this could be cached better
        CompoundId oxytoxyId = SimulationParameters::compoundRegistry().getTypeId("oxytoxy");

        // This list is quite expensive to build each frame but
        // there's currently no good way to cache this
        array<ObjectID>@ allMicrobes = world.GetScriptComponentHolder(
            "MicrobeComponent").GetIndex();

        for(uint i = 0; i < CachedComponents.length(); ++i){

            MicrobeAISystemCached@ components = CachedComponents[i];

            ObjectID microbeEntity = components.entity;
            MicrobeAIControllerComponent@ aiComponent = components.first;
            MicrobeComponent@ microbeComponent = components.second;
            Position@ position = components.third;
    // ai interval
            aiComponent.intervalRemaining += logicTime;
            while(aiComponent.intervalRemaining > aiComponent.reevalutationInterval) {
                aiComponent.intervalRemaining -= aiComponent.reevalutationInterval;
                int numberOfAgentVacuoless = int(
                    microbeComponent.specialStorageOrganelles[formatUInt(oxytoxyId)]);

    prey = getNearestPreyItem(components);
    predator = getNearestPredatorItem(components);

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

                //         if(this.predator !is null){ // for running away from the predadtor
                //             auto predatorSceneNodeComponent = getComponent(this.predator,
                //                 OgreSceneNodeComponent);
                //             microbeComponent.facingTargetPoint =
            //                 Vector3(-predatorSceneNodeComponent.transform.position.x,
            //                     -predatorSceneNodeComponent.transform.position.y, 0);
            //             microbeComponent.movementDirection = Vector3(0, AI_MOVEMENT_SPEED, 0);
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
    //do run and tumble
    doRunAndTumble(components);
            }
        }
    }

    // For getting the nearest prey item
    ObjectID getNearestPreyItem(MicrobeAISystemCached@ components){
    // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

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
    return NULL_OBJECT;
    }

    // For getting the nearest predator
    ObjectID getNearestPredatorItem(MicrobeAISystemCached@ components){
    // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

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
    return NULL_OBJECT;
    }

    // For doing run and tumble
    void doRunAndTumble(MicrobeAISystemCached@ components){
    // Set Components
        ObjectID microbeEntity = components.entity;
        MicrobeAIControllerComponent@ aiComponent = components.first;
        MicrobeComponent@ microbeComponent = components.second;
        Position@ position = components.third;

        // This is just temporary so i can look at how bacteria look stationary for testing the new texture.
        if (!MicrobeOperations::getSpeciesComponent(world, microbeEntity).isBacteria)
            {
            // Target position
            Float3 targetPosition = Float3(0, 0, 0);
            //make AI move randomly for now
            auto randAngle = GetEngine().GetRandom().GetFloat(0, 2*PI);
            auto randDist = GetEngine().GetRandom().GetFloat(10,aiComponent.movementRadius);
            targetPosition = Float3(cos(randAngle) * randDist,0, sin(randAngle)* randDist);
            auto vec = (targetPosition - position._Position);
            aiComponent.direction = vec.Normalize();
            microbeComponent.facingTargetPoint = targetPosition;
            microbeComponent.movementDirection = Float3(0, 0, -AI_MOVEMENT_SPEED);
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

    // It's really silly to have these here instead of in the AI
    // component, like what system tries to store most of its state in
    // itself instead of its components?
    // dictionary preyCandidates = {};
    // // Used for removing from preyCandidates
    // dictionary preyEntityToIndexMap = {};
    // int currentPreyIndex = 0;

    // //counting number of frames so the prey get updated the fittest prey
    // int preycount = 0;
    // //checking if the prey escaped
    // bool preyEscaped = false;

    // the final predator the cell shall run from
    ObjectID predator = -1;

    // the final prey the cell should hunt
    ObjectID prey = -1;

    //i need it to be very big for now it will get changed
    int preyMaxHitpoints = 100000;

}
