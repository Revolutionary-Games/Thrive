// The current biome's id. This is initially set this high because now
// the biome is only changed if the number changed. So if there ever
// are more biomes than this initial value something will break.
uint64 currentBiome = 1000000;

const Biome@ getCurrentBiome(){

    return SimulationParameters::biomeRegistry().getTypeData(currentBiome);
}

ObjectID createCompoundCloud(CellStageWorld@ world, CompoundId compound,
    float x, float z, float amount
) {
    if(amount == 0){
        amount = getCurrentBiome().getCompound(compound).amount;
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

class CloudFactory{

    CloudFactory(CompoundId c){

        compound = c;
    }

    ObjectID spawn(CellStageWorld@ world, Float3 pos){
        createCompoundCloud(world, compound, pos.X+2, pos.Z, 0);
        createCompoundCloud(world, compound, pos.X-2, pos.Z, 0);
        createCompoundCloud(world, compound, pos.X, pos.Z+2, 0);
        createCompoundCloud(world, compound, pos.X, pos.Z-2, 0);
        return createCompoundCloud(world, compound, pos.X, pos.Z, 0);
    }

    private CompoundId compound;
}

class Chunkfactory{

    Chunkfactory(uint c){

        chunkId = c;
    }

    ObjectID spawn(CellStageWorld@ world, Float3 pos){
        return createChunk(world, chunkId, pos);
    }

    private uint chunkId;
}

dictionary compoundSpawnTypes;
dictionary chunkSpawnTypes;

// Setting the current biome to the one with the specified name.
void setBiome(uint64 biomeId, CellStageWorld@ world){
    assert(world !is null, "setBiome requires world");

    LOG_INFO("Setting biome to: " + biomeId);
    // Getting the base biome to change to.
    currentBiome = biomeId;
    auto biome = getCurrentBiome();

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
    //Update biome for process system
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

// Setting the current biome to a random biome selected from the biome table.
void setPatchBiome(CellStageWorld@ world)
{
    LOG_INFO("setting to patch biome");

    // Getting the size of the biome table.
    // Selecting a random biome.
    uint64 biome = GetThriveGame().getPatchManager().getCurrentPatch().getBiome();

    // Switching to that biome if we arent in that biome already
    if (currentBiome != biome)
    {
        setBiome(biome, world);
    }
}
