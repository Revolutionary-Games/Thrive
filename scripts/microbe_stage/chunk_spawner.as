// Factory for chunks and helpers for spawning the right compound clouds for the current patch

ObjectID spawnChunk(CellStageWorld@ world, const ChunkData@ chunk, const Float3 &in pos)
{
    // chunk
    ObjectID chunkEntity = world.CreateEntity();

    //Position and render node
    auto position = world.Create_Position(chunkEntity, pos,
        Quaternion(Float3(0, 1, 1), Degree(GetEngine().GetRandom().GetNumber(0, 360))));


    auto renderNode = world.Create_RenderNode(chunkEntity);
    // Grab scale from json
    double chunkScale = chunk.chunkScale;
    renderNode.Scale = Float3(chunkScale, chunkScale, chunkScale);
    renderNode.Marked = true;
    renderNode.Node.SetOrientation(Quaternion(Float3(0, 1, 1),
            Degree(GetEngine().GetRandom().GetNumber(0, 360))));

    renderNode.Node.SetPosition(pos);

    //Grab data
    double ventAmount= chunk.ventAmount;
    bool dissolves=chunk.dissolves;
    int radius = chunk.radius;
    int mass = chunk.mass;
    int chunkSize = chunk.size;
    auto meshListSize = chunk.getMeshListSize();
    int selectedIndex = GetEngine().GetRandom().GetNumber(0, meshListSize-1);
    string mesh = chunk.getMesh(selectedIndex)+".fbx";
    string texture = chunk.getTexture(selectedIndex);

    //Set things
    auto venter = world.Create_CompoundVenterComponent(chunkEntity);
    venter.setVentAmount(ventAmount);
    venter.setDoDissolve(dissolves);
    auto bag = world.Create_CompoundBagComponent(chunkEntity);
    auto engulfable = world.Create_EngulfableComponent(chunkEntity);
    engulfable.setSize(chunkSize);


    auto chunkCompounds = chunk.getCompoundKeys();
    //LOG_INFO("chunkCompounds.length = " + chunkCompounds.length());

    for(uint i = 0; i < chunkCompounds.length(); ++i){
        auto compoundId = SimulationParameters::compoundRegistry().getTypeData(chunkCompounds[i]).id;
        //LOG_INFO("got here:");
        // And register new
        const double amount = chunk.getCompound(chunkCompounds[i]).amount;
        //LOG_INFO("amount:"+amount);
        bag.setCompound(compoundId,amount);
    }

    auto model = world.Create_Model(chunkEntity, mesh, getBasicMaterialWithTexture(
            texture));

    // Fluid mechanics.
    world.Create_FluidEffectComponent(chunkEntity);

    // Rigid Body
    auto rigidBody = world.Create_Physics(chunkEntity, position);

    //chunk properties
    if (chunk.damages > 0.0f || chunk.deleteOnTouch){
        auto damager = world.Create_DamageOnTouchComponent(chunkEntity);
        damager.setDamage(chunk.damages);
        damager.setDeletes(chunk.deleteOnTouch);
        //Damage
        auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
            world.GetPhysicalWorld().CreateSphere(radius),mass,
            world.GetPhysicalMaterial("chunkDamageMaterial"));

        body.ConstraintMovementAxises();
    }
    else {
        auto body = rigidBody.CreatePhysicsBody(world.GetPhysicalWorld(),
            world.GetPhysicalWorld().CreateSphere(radius),mass,
            //engulfable
            world.GetPhysicalMaterial("engulfableMaterial"));
        body.ConstraintMovementAxises();
    }

    rigidBody.JumpTo(position);

    return chunkEntity;
}
