// Global table which stores the current biome the player is in.
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

        return createCompoundCloud(world, compound, pos.X, pos.Z, 0);
    }

    private CompoundId compound;
}

dictionary compoundSpawnTypes;

// Setting the current biome to the one with the specified name.
void setBiome(uint64 biomeId, CellStageWorld@ world){
    assert(world !is null, "setBiome requires world");

    LOG_INFO("setting biome to: " + biomeId);
    // Getting the base biome to change to.
    currentBiome = biomeId;

    auto biome = getCurrentBiome();

    auto biomeCompounds = biome.getCompoundKeys();
    LOG_INFO("biomeCompounds.length = " + biomeCompounds.length());
    for(uint i = 0; i < biomeCompounds.length(); ++i){
        auto compoundId = biomeCompounds[i];

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
}

void setSunlightForBiome(CellStageWorld@ world){
    // Light properties isnt working for some reason
    world.SetLightProperties(getCurrentBiome().diffuseColors, getCurrentBiome().specularColors,
        Ogre::Vector3(Float3(0.55f, -0.3f, 0.75f).Normalize()), 30,
        // https://ogrecave.github.io/ogre/api/2.1/class_ogre_1_1_scene_manager.html#a56cd9aa2c4dee4eec9eb07ce1372fb52
        Ogre::ColourValue(0.3f, 0.3f, 0.3f),
        Ogre::ColourValue(0.2f, 0.2f, 0.2f),
        -Float3(0.55f, -0.3f, 0.75f).Normalize() + Float3::UnitVUp * 0.2f
    );
    // These work fine
    LOG_INFO("Diffuse Colours For Biome r:" + getCurrentBiome().diffuseColors.r +
        "g:" + getCurrentBiome().diffuseColors.g + "b:" + getCurrentBiome().diffuseColors.b);
    LOG_INFO("specular Colours For Biome r:" + getCurrentBiome().diffuseColors.r +
        "g:" + getCurrentBiome().specularColors.g + "b:" + getCurrentBiome().specularColors.b);

    // Diffused gasses percenatge
   LOG_INFO("Diffused Oxygen For Biome " + getCurrentBiome().oxygenPercentage);
   LOG_INFO("Diffused C02 For Biome " + getCurrentBiome().carbonDioxidePercentage);

}

// Setting the current biome to a random biome selected from the biome table.
void setRandomBiome(CellStageWorld@ world){
     LOG_INFO("setting random biome");
    // Getting the size of the biome table.
    // Selecting a random biome.
    auto biome = GetEngine().GetRandom().GetNumber(0,
        int(SimulationParameters::biomeRegistry().getSize()-1));

    // Switching to that biome.
    setBiome(biome, world);
}
