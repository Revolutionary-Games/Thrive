// Common operations that work with just shared definitions

namespace BaseMicrobeOperations{

// Getter for microbe species
//
// returns the species component or null if species with that name doesn't exist
shared SpeciesComponent@ getSpeciesComponent(const string &in speciesName)
{
    auto world = GetThriveGame().getCellStage();
    
    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, speciesName);

    return world.GetComponent_SpeciesComponent(entity);
}

// Getter for species processor component
//
// returns the processor component or null if such species doesn't have that component
// TODO: check what calls this and make it store the species entity id if it also calls
// getSpeciesComponent to save searching the whole species component index multiple times
shared ProcessorComponent@ getProcessorComponent(const string &in speciesName)
{
    auto world = GetThriveGame().getCellStage();
    
    // This needs to loop all the components and get the matching one
    auto entity = findSpeciesEntityByName(world, speciesName);

    return world.GetComponent_ProcessorComponent(entity);
}


}

