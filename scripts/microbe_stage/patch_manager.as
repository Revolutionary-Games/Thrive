// Handles setting up spawns etc. for the microbe stage for the current
// Also runs the patch map generator

// Makes sure the currently selected player patch is correctly setup
// Should be called each time when entering the stage from either the menu or the editor
void runPatchSetup(CellStageWorld@ world)
{
    assert(world !is null, "runPatchSetup requires world");

    PatchManager::enablePatchSettings(world);

    LOG_INFO("Current patch is: " + -1);
    // Getting the base biome to change to.
    currentBiome = biomeId;
    auto biome = getCurrentBiome();
}

PatchMap@ generatePatchMap()
{

}

void setActivePatchMap(PatchMap@ map)
{

}

void setCurrentPlayerPatch(int id)
{

}

namespace PatchManager{


void enablePatchSettings(CellStageWorld@ world){

    auto chunks = biome.getChunkKeys();
    LOG_INFO("chunks.length = " + chunks.length());

    // clearing chunks (all of them)
    for (uint c = 0; c < chunkSpawnTypes.getSize(); ++c){
        const string typeStr = formatUInt(c);
        if(chunkSpawnTypes.exists(typeStr)){
            world.GetSpawnSystem().removeSpawnType(SpawnerTypeId(
                    chunkSpawnTypes[typeStr]));
            LOG_INFO("deleting chunk spawn");
        }
    }

    for(uint i = 0; i < chunks.length(); ++i){
        auto chunkId = chunks[i];
        Chunkfactory@ spawnChunk = Chunkfactory(chunkId);
        const string typeStr = formatUInt(chunkId);
        // And register new
        const auto density = biome.getChunk(chunkId).density;
        const auto name = biome.getChunk(chunkId).name;

        if(density <= 0){
            LOG_WARNING("chunk spawn density is 0. It won't spawn");
        }

        LOG_INFO("registering chunk: " + chunkId + " Name: "+name +" density: " + density);
        SpawnFactoryFunc@ factory = SpawnFactoryFunc(spawnChunk.spawn);
        chunkSpawnTypes[typeStr] = world.GetSpawnSystem().addSpawnType(factory, density,
            MICROBE_SPAWN_RADIUS);

    }

    auto biomeCompounds = biome.getCompoundKeys();
    LOG_INFO("biomeCompounds.length = " + biomeCompounds.length());
    for(uint i = 0; i < biomeCompounds.length(); ++i){
        auto compoundId = SimulationParameters::compoundRegistry().getTypeData(biomeCompounds[i]).id;

        if(SimulationParameters::compoundRegistry().getTypeData(compoundId).isCloud){

            CloudFactory@ spawnCloud = CloudFactory(compoundId);

            const string typeStr = formatUInt(compoundId);

            // Remove existing (if there is one)
            if(compoundSpawnTypes.exists(typeStr)){
                world.GetSpawnSystem().removeSpawnType(SpawnerTypeId(
                        compoundSpawnTypes[typeStr]));
            }

            // And register new
            const auto density = biome.getCompound(biomeCompounds[i]).density;

            if(density <= 0){
                LOG_WARNING("Compound density is 0. It won't spawn");
            }

            LOG_INFO("registering cloud: " + compoundId + ", density: " + density);

            SpawnFactoryFunc@ factory = SpawnFactoryFunc(spawnCloud.spawn);

            compoundSpawnTypes[typeStr] = world.GetSpawnSystem().addSpawnType(
                factory, density,
                CLOUD_SPAWN_RADIUS);
        }
    }
    
    // Change the lighting
    setSunlightForBiome(world);
    // Changing the background.
    GetThriveGame().setBackgroundMaterial(biome.background);
    // Update environment for process system
    world.GetProcessSystem().setProcessBiome(biomeId);

    // Update oxygen and carbon dioxide numbers
    auto oxyId = SimulationParameters::compoundRegistry().getTypeId("oxygen");
    auto c02Id = SimulationParameters::compoundRegistry().getTypeId("carbondioxide");
    auto n2Id = SimulationParameters::compoundRegistry().getTypeId("nitrogen");
    GenericEvent@ updateDissolvedGasses = GenericEvent("UpdateDissolvedGasses");
    NamedVars@ vars = updateDissolvedGasses.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("oxygenPercent",
            world.GetProcessSystem().getDissolved(oxyId)*100));
    vars.AddValue(ScriptSafeVariableBlock("co2Percent",
            world.GetProcessSystem().getDissolved(c02Id)*100));
    vars.AddValue(ScriptSafeVariableBlock("n2Percent",
            world.GetProcessSystem().getDissolved(n2Id)*100));
    GetEngine().GetEventHandler().CallEvent(updateDissolvedGasses);
}

void setSunlightForBiome(CellStageWorld@ world){
    // TODO: redo
    // Light properties isnt working for some reason
    // world.SetLightProperties(getCurrentBiome().diffuseColors, getCurrentBiome().specularColors,
    //     bs::Vector3(Float3(0.55f, -0.3f, 0.75f).Normalize()), getCurrentBiome().lightPower,
    //     // https://ogrecave.github.io/ogre/api/2.1/class_ogre_1_1_scene_manager.html#a56cd9aa2c4dee4eec9eb07ce1372fb52
    //     getCurrentBiome().upperAmbientColor,
    //     getCurrentBiome().lowerAmbientColor,
    //     -Float3(0.55f, -0.3f, 0.75f).Normalize() + Float3::UnitVUp * 0.2f
    // );
}

}

