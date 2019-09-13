// Cloud factory and helpers for spawning the right compound clouds for the current patch
// The agents aren't a cloud yet but they are also here

// Places a new blob of compound at the specified location
ObjectID createCompoundCloud(CellStageWorld@ world, CompoundId compound,
    float x, float z, float amount
) {
    if(amount <= 0){
        LOG_ERROR("createCompoundCloud amount is <= 0");
    }

    // This is just a sanity check
    //if(compoundTable[compoundName] and compoundTable[compoundName].isCloud)

    // addCloud requires integer arguments. This is not true anymore
    int roundedX = round(x);
    int roundedZ = round(z);

    // TODO: this isn't the best way to handle this for max performance
    world.GetCompoundCloudSystem().addCloud(compound, amount, Float3(roundedX, 0, roundedZ));

    // We don't spawn new entities
    return NULL_OBJECT;
}

//! Spawn system compound spawn
void spawnCompoundCloud(CellStageWorld@ world, CompoundId compound, float amount,
    const Float3 &in pos)
{
    createCompoundCloud(world, compound, pos.X+2, pos.Z, amount);
    createCompoundCloud(world, compound, pos.X-2, pos.Z, amount);
    createCompoundCloud(world, compound, pos.X, pos.Z+2, amount);
    createCompoundCloud(world, compound, pos.X, pos.Z-2, amount);
    createCompoundCloud(world, compound, pos.X, pos.Z, amount);
}


// ------------------------------------ //
// Agents
void createAgentCloud(CellStageWorld@ world, CompoundId compoundId,
    Float3 pos, Float3 direction, float amount, float lifetime,
    string speciesName, ObjectID creatorEntity)
{
    auto normalizedDirection = direction.Normalize();
    auto agentEntity = world.CreateEntity();

    auto position = world.Create_Position(agentEntity, pos + (direction * 1.5),
        bs::Quaternion(bs::Degree(GetEngine().GetRandom().GetNumber(0, 360)),
            bs::Vector3(0,1, 0)));

    // Agent
    auto agentProperties = world.Create_AgentProperties(agentEntity);
    agentProperties.setSpeciesName(speciesName);
    agentProperties.setParentEntity(creatorEntity);
    agentProperties.setAgentType("oxytoxy");

    auto rigidBody = world.Create_Physics(agentEntity, position);


    auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
        world.GetPhysicalWorld().CreateSphere(HEX_SIZE), 0.5,
        world.GetPhysicalMaterial("agentCollision"));

    body.ConstraintMovementAxises();

    // TODO: physics property applying here as well
    // rigidBody.properties.friction = 0.4;
    // rigidBody.properties.linearDamping = 0.4;

    body.SetVelocity(normalizedDirection * AGENT_EMISSION_VELOCITY);
    rigidBody.JumpTo(position);
    auto sceneNode = world.Create_RenderNode(agentEntity);
    auto model = world.Create_Model(agentEntity, "oxytoxy.fbx",
        getBasicMaterialWithTexture("oxytoxy_fluid.png"));

    // // Need to set the tint
    // model.GraphicalObject.setCustomParameter(1, bs::Vector4(1, 1, 1, 1));

    auto timedLifeComponent = world.Create_TimedLifeComponent(agentEntity, int(lifetime));
}
