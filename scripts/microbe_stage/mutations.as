// Functions for generating mutated versions of Species

//! Creates a fully random species
Species@ createRandomSpecies(int steps = 5)
{
    array<PlacedOrganelle@> organelles;
    organelles.insertLast(PlacedOrganelle(
            getOrganelleDefinition("cytoplasm"), 0, 0, 0));

    Species@ current = Species::createSpecies("random", generateNameSection(),
        generateNameSection(), organelles, Float4(1, 1, 1, 1), true, "single", 0.5f,
        DEFAULT_INITIAL_COMPOUNDS,
        100.0f, 100.0f, 100.0f, 200.0f, 100.0f);

    for(int step = 0; step < steps; ++step){
        @current = createMutatedSpecies(current);
    }

    return current;
}

//! Creates a mutated version of the species
Species@ createMutatedSpecies(Species@ parent)
{
    bool isBacteria = parent.isBacteria;
    string name = !isBacteria ? randomSpeciesName() : randomBacteriaName();

    string epithet;

    //Mutate the epithet
    if (GetEngine().GetRandom().GetNumber(0, 100) < MUTATION_WORD_EDIT){
        epithet = MutationHelpers::mutateWord(parent.epithet);
    }
    else {
        epithet = generateNameSection();
    }

    string genus = parent.genus;

    // Variables used in AI to determine general behavior mutate these
    // This used to be a method
    float aggression = parent.aggression+GetEngine().GetRandom().GetFloat(
        MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
    float fear = parent.fear+GetEngine().GetRandom().GetFloat(
        MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
    float activity = parent.activity+GetEngine().GetRandom().GetFloat(
        MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
    float focus = parent.focus+GetEngine().GetRandom().GetFloat(
        MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);
    float opportunism = parent.opportunism+GetEngine().GetRandom().GetFloat(
        MIN_SPECIES_PERSONALITY_MUTATION, MAX_SPECIES_PERSONALITY_MUTATION);

    // Make sure not over or under our scales
    // This used to be a method as well
    aggression = clamp(aggression, 0.0f, MAX_SPECIES_AGRESSION);
    fear = clamp(fear, 0.0f, MAX_SPECIES_FEAR);
    activity = clamp(activity, 0.0f, MAX_SPECIES_ACTIVITY);
    focus = clamp(focus, 0.0f, MAX_SPECIES_FOCUS);
    opportunism = clamp(opportunism, 0.0f, MAX_SPECIES_OPPORTUNISM);

    if (GetEngine().GetRandom().GetNumber(0,100) <= MUTATION_CHANGE_GENUS)
    {
        // We can do more fun stuff here later
        if (GetEngine().GetRandom().GetNumber(0, 100) < MUTATION_WORD_EDIT){
            genus = MutationHelpers::mutateWord(parent.genus);
        }
        else {
            genus = generateNameSection();
        }
    }

    string stringCode = mutateMicrobe(parent.stringCode, isBacteria);

    // There is a small chance of evolving into a eukaryote
    if (stringCode.findFirst("N") >= 0){
        isBacteria=false;
        name = randomSpeciesName();
    }

    Float4 colour = isBacteria ? MutationHelpers::randomProkayroteColour() :
        MutationHelpers::randomColour();

    // This used to be a method
    string membraneType = "single";
    if (GetEngine().GetRandom().GetNumber(0,100)<=20){ // Could perhaps use a weighted entry model here... the earlier one is listed, the more likely currently (I think). That may be an issue.
        if (GetEngine().GetRandom().GetNumber(0,100) < 50){
            membraneType = "single";
        }
        else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
            membraneType = "double";
            colour.W = MutationHelpers::randomOpacityChitin(); // Why on double? Should this be on cellulose instead?
        }
        else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
            membraneType = "cellulose";
        }
        else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
            membraneType = "chitin";
            colour.W = MutationHelpers::randomOpacityChitin();
        }
        else if (GetEngine().GetRandom().GetNumber(0,100) < 50) {
            membraneType = "calcium_carbonate";
            colour.W = MutationHelpers::randomOpacityChitin();
        }
        else {
            membraneType = "silica";
            colour.W = MutationHelpers::randomOpacityChitin();
        }
    }
    else{
        membraneType = SimulationParameters::membraneRegistry().getInternalName(parent.membraneType);
    }

    float membraneRigidity = GetEngine().GetRandom().GetNumber(0, 100) / 100.0f;

    // This translates the genetic code into positions
    auto organelles = positionOrganelles(stringCode);

    const dictionary@ initialCompounds;

    // If you have iron (f is the symbol for rusticyanin)
    if (stringCode.findFirst('f') >= 0)
    {
        @initialCompounds = DEFAULT_INITIAL_COMPOUNDS_IRON;
    }
    else if (stringCode.findFirst('C') >= 0 || stringCode.findFirst('c') >= 0)
    {
        @initialCompounds = DEFAULT_INITIAL_COMPOUNDS_CHEMO;
    }
    else {
        @initialCompounds = DEFAULT_INITIAL_COMPOUNDS;
    }

    Species@ newSpecies = Species::createSpecies(name, genus, epithet,
        organelles, colour, isBacteria, membraneType, membraneRigidity,
        initialCompounds, aggression, fear, activity, focus, opportunism);

    return newSpecies;
}

namespace Unused{
//! \param updateSpecies will be modified to match the organelles of the microbe
// This isnt used anywhere by the way
void updateSpeciesFromMicrobe(CellStageWorld@ world, ObjectID microbeEntity,
    Species@ updateSpecies)
{
    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
    auto membraneComponent = world.GetComponent_MembraneComponent(microbeEntity);

    // this.name = microbeComponent.speciesName
    updateSpecies.colour = membraneComponent.getColour();


    updateSpecies.organelles.resize(0);

    // Create species' organelle data
    for(uint i = 0; i < microbeComponent.organelles.length(); i++){

        updateSpecies.organelles.insertLast(PlacedOrganelle(microbeComponent.organelles[i]));
    }

    // This microbes compound amounts will be the new population average.
    updateSpecies.avgCompoundAmounts = {};

    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint compoundId = 0; compoundId < compoundCount; ++compoundId){

        auto amount = MicrobeOperations::getCompoundAmount(world, microbeEntity, compoundId);
        updateSpecies.avgCompoundAmounts[formatUInt(compoundId)] = InitialCompound(amount);
    }
    // TODO: make this update the ProcessorComponent based on microbe thresholds
}

}
