//Global table which stores the current biome the player is in.
uint64 currentBiome = 0;

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
    //if(compoundTable[compoundName] and compoundTable[compoundName].isCloud){
    
    // addCloud requires integer arguments
    int roundedX = round(x);
    int roundedZ = round(z);

    // TODO: this isn't the best way to handle this for max performance
    auto environmentCloud = world.GetComponent_CompoundCloudComponent(
        findCompoundCloudByCompound(world, compound));


    environmentCloud.addCloud(amount, roundedX, roundedZ);
    
    // We don't spawn new entities
    return NULL_OBJECT;
}

class CloudFactory{

    CloudFactory(CompoundId c){

        compound = c;
    }

    ObjectID spawn(CellStageWorld@ world, Float3 pos){

        return createCompoundCloud(world, compound, pos.X, pos.Z, 0);
    }

    private CompoundId compound;
}

dictionary compoundSpawnTypes;

//Setting the current biome to the one with the specified name.
void setBiome(uint64 biomeId, CellStageWorld@ world){
    assert(world !is null, "setBiome requires world");
        
    //Getting the base biome to change to.
    currentBiome = biomeId;

    auto biome = getCurrentBiome();

    auto biomeCompounds = biome.getCompoundKeys();
    for(uint i = 0; i < biomeCompounds.length(); ++i){

        auto compoundId = biomeCompounds[i];

        if(SimulationParameters::compoundRegistry().getTypeData(compoundId).isCloud){

            CloudFactory@ spawnCloud = CloudFactory(compoundId);

            // Remove existing (if there is one)
            world.GetSpawnSystem().removeSpawnType(SpawnerTypeId(compoundSpawnTypes[
                        formatInt(compoundId)]));
            
            SpawnFactoryFunc@ factory = SpawnFactoryFunc(spawnCloud.spawn);

            // And register new
            compoundSpawnTypes[formatInt(compoundId)] = world.GetSpawnSystem().addSpawnType(
                factory, biome.getCompound(biomeCompounds[i]).density,
                CLOUD_SPAWN_RADIUS);
        }
    }

    //Changing the background.
    auto entity = GetThriveGame().m_backgroundPlane;

    auto plane = world.GetComponent_Plane(entity);

    plane.GraphicalObject.setMaterial(biome.background);
}

//Setting the current biome to a random biome selected from the biome table.
void setRandomBiome(CellStageWorld@ world){
    //Getting the size of the biome table.
    //Selecting a random biome.
    auto biome = GetEngine().GetRandom().GetNumber(0,
        int(SimulationParameters::biomeRegistry().getSize() - 1));

    //Switching to that biome.
    setBiome(biome, world);
}
