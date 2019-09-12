// Cloud factory and helpers for spawning the right compound clouds for the current patch

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


namespace CompoundCloudSpawner{


class CloudFactory{
    CloudFactory(CompoundId c, float amount_)
    {
        compound = c;
        amount = amount_;
    }

    ObjectID spawn(CellStageWorld@ world, Float3 pos)
    {
        createCompoundCloud(world, compound, pos.X+2, pos.Z, amount);
        createCompoundCloud(world, compound, pos.X-2, pos.Z, amount);
        createCompoundCloud(world, compound, pos.X, pos.Z+2, amount);
        createCompoundCloud(world, compound, pos.X, pos.Z-2, amount);
        return createCompoundCloud(world, compound, pos.X, pos.Z, amount);
    }

    private CompoundId compound;
    private float amount;
}

dictionary compoundSpawnTypes;


}
