#include "configs.as"


// This is a helper for calling all the setup functions at the same time
// This is the one called from C++
void setupScriptsForWorld(CellStageWorld@ world)
{
    setupSystemsForWorld(world);
}

//! Server variant of setupScriptsForWorld
void setupScriptsForWorld_Server(CellStageWorld@ world)
{
    setupSystemsForWorld_Server(world);
}

//! Client variant of setupScriptsForWorld
void setupScriptsForWorld_Client(CellStageWorld@ world)
{
    setupSystemsForWorld_Client(world);
}

ScriptComponent@ MicrobeComponentFactory(GameWorld@ world)
{
    return MicrobeComponent();
}

ScriptComponent@ MicrobeAIControllerComponentFactory(GameWorld@ world)
{
    return MicrobeAIControllerComponent();
}

//! This function instantiates all script system types for a world
//! and registers all the microbe components that are defined in scripts to work
//! in a world
void setupSystemsForWorld(CellStageWorld@ world)
{
    // Fail if compound registry is empty (hud system caches the compound ids on startup) //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);
    world.RegisterScriptComponentType("MicrobeAIControllerComponent",
        @MicrobeAIControllerComponentFactory);

    // Add any new systems and component types that are defined in scripts here
    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
    world.RegisterScriptSystem("MicrobeAISystem", MicrobeAISystem());

    // Add world effects
    world.GetTimedWorldOperations().registerEffect("reduce glucose over time",
        @reduceGlucoseOverTime);
    world.GetTimedWorldOperations().registerEffect("update patch gasses",
        @updatePatchGasses);
}

//! Server variant of setupSystemsForWorld
void setupSystemsForWorld_Server(CellStageWorld@ world)
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);
    world.RegisterScriptComponentType("MicrobeAIControllerComponent",
        @MicrobeAIControllerComponentFactory);

    // Add any new systems and component types that are defined in scripts here
    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeAISystem", MicrobeAISystem());
}

//! Client variant of setupSystemsForWorld
void setupSystemsForWorld_Client(CellStageWorld@ world)
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    world.RegisterScriptComponentType("MicrobeComponent", @MicrobeComponentFactory);

    world.RegisterScriptSystem("MicrobeSystem", MicrobeSystem());
    world.RegisterScriptSystem("MicrobeStageHudSystem", MicrobeStageHudSystem());
}


//! This spawns the player
void setupPlayer(CellStageWorld@ world)
{
    assert(world !is null);
    GetThriveGame().playerData().lockedMap().addLock("Toxin");
    GetThriveGame().playerData().lockedMap().addLock("chloroplast");

    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");

    GetThriveGame().playerData().setActiveCreature(microbe);
}

//! This spawns a player in multiplayer
ObjectID spawnPlayer_Server(CellStageWorld@ world)
{
    ObjectID microbe = MicrobeOperations::spawnMicrobe(world, Float3(0, 0, 0), "Default",
        false);

    assert(microbe != NULL_OBJECT, "Failed to spawn player cell");
    return microbe;
}

//! This handles making a cell out of an entity received from the server
void setupClientSideReceivedCell(CellStageWorld@ world, ObjectID entity)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Create(entity));

    auto speciesName = "Default";

    auto species = MicrobeOperations::getSpecies(world, speciesName);

    assert(species !is null);

    microbeComponent.init(entity, true, species);

    auto shape = world.GetPhysicalWorld().CreateCompound();
    Species::applyTemplate(world, entity, species, shape);

    auto rigidBody = world.GetComponent_Physics(entity);

    MicrobeOperations::_applyMicrobeCollisionShape(world, rigidBody, microbeComponent, shape);

    microbeComponent.initialized = true;
}
// ------------------------------------ //
void onReturnFromEditor(CellStageWorld@ world)
{
    // Increase the population by 30 and increase the generation
    auto playerSpecies = MicrobeOperations::getSpecies(world, "Default");

    const auto player = GetThriveGame().playerData().activeCreature();

    // Sanity check
    assert(playerSpecies is MicrobeOperations::getSpecies(world, player));

    ++playerSpecies.generation;

    // Call event that checks win conditions
    if(!GetThriveGame().playerData().isFreeBuilding()){
        GenericEvent@ event = GenericEvent("CheckWin");
        NamedVars@ vars = event.GetNamedVars();
        vars.AddValue(ScriptSafeVariableBlock("generation", playerSpecies.generation));
        vars.AddValue(ScriptSafeVariableBlock("population", playerSpecies.population));
        GetEngine().GetEventHandler().CallEvent(event);
    }

    // The editor changes the cell template for the species so we won't have to do that here
    auto pos = world.GetComponent_Position(player);

    assert(pos !is null);

    // Spawn another cell from the player species
    auto membraneComponent = world.GetComponent_MembraneComponent(player);

    // Offset between cells
    pos._Position.X += membraneComponent.calculateEncompassingCircleRadius();
    pos._Position.Z += membraneComponent.calculateEncompassingCircleRadius();
    pos.Marked = true;

    ObjectID sisterCell = MicrobeOperations::spawnMicrobe(world, pos._Position, "Default",
        true);

    // Make it despawn like normal
    world.Create_SpawnedComponent(sisterCell, MICROBE_SPAWN_RADIUS *
        MICROBE_SPAWN_RADIUS);

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(player));

    // Reset the player cell to be the same as the species template
    // This also sets the processor component
    Species::restoreOrganelleLayout(world, player, microbeComponent, playerSpecies);

    // Reset Players reproduction
    microbeComponent.reproductionStage = 0;

    // Halve the players Compounds
    for(uint64 compoundID = 0; compoundID <
            SimulationParameters::compoundRegistry().getSize(); ++compoundID)
    {
        auto amount = MicrobeOperations::getCompoundAmount(world, player,
            compoundID);

        if(amount != 0){
            MicrobeOperations::takeCompound(world, player, compoundID,
                amount / 2.0f /*, false*/ );
        }
    }

}
