class Biome{

    string name;
    float temperature;
    float sunlight;
    string background;
    // Contains amounts indexed by compound name
    dictionary compounds;
}

//Global table which stores the current biome the player is in.
Biome@ currentBiome = null;

ObjectID createCompoundCloud(CellStageWorld@ world, CompoundId compound,
    float x, float z, float amount
) {
    if(amount == 0){
        amount = float(currentBiome.compounds[compoundName]);
    }

    // This is just a sanity check
    //if(compoundTable[compoundName] and compoundTable[compoundName].isCloud){
    
    // addCloud requires integer arguments
    int roundedX = floor(x);
    int roundedY = floor(y);

    // TODO: this isn't the best way to handle this for max performance
    environmentCloud = world.GetComponent_CompoundCloudComponent(
        findCompoundCloudByCompound(world, compound));


    environmentCloud.addCloud(amount, roundedX, roundedY);
    
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
void setBiome(const string &in biomeName, CellStageWorld@ world){
    assert(world !is null, "setBiome requires world");
        
    //Getting the base biome to change to.
    auto baseBiome = biomeTable[biomeName];

    //Setting the new biome attributes
    currentBiome = Biome();
    currentBiome.name = biomeName;
    currentBiome.temperature = baseBiome.temperature;
    currentBiome.sunlight = baseBiome.sunlight;
    currentBiome.background = baseBiome.background;
    currentBiome.compounds = {};

    for(compoundName, compoundData in pairs(baseBiome.compounds)){
        currentBiome.compounds[compoundName] = compoundData.amount;

        if(compoundTable[compoundName].isCloud){

            CloudFactory@ spawnCloud = CloudFactory(compoundName);
            
            gSpawnSystem.removeSpawnType(compoundSpawnTypes[compoundName]);
            compoundSpawnTypes[compoundName] = gSpawnSystem.addSpawnType(spawnCloud.spawn,
                compoundData.density, CLOUD_SPAWN_RADIUS);            
        }
    }

    //Changing the background.
    auto entity = GetThriveGame().m_backgroundPlane;

    auto plane = world.GetComponent_Plane(entity);

    plane.GraphicalObject.setMaterial(currentBiome.background);
}

//Setting the current biome to a random biome selected from the biome table.
void setRandomBiome(CellStageWorld@ world){
    //Getting the size of the biome table.
    auto keys = biomeTable.getKeys();

    //Selecting a random biome.
    auto currentBiomeName = biomeNameTable[keys[
            GetEngine().GetRandom().GetValue(0, keys.length() - 1)]];

    //Switching to that biome.
    setBiome(currentBiomeName, world);
}
