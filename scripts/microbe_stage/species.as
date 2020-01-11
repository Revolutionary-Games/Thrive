// Species related operation functions
namespace Species{

// Given a newly-created microbe, this sets the organelles and all other
// species-specific microbe data like agent codes, for example.
//! Brief applies template to a microbe entity making sure it has all
//! the correct organelle components
//! \param editShape if the physics body is not created yet this function can directly
//! edit the shape without trying to alter the body
void applyTemplate(CellStageWorld@ world, ObjectID microbe, Species@ species,
    PhysicsShape@ editShape = null)
{
    // Fail if the species is not set up
    assert(species.organelles.length() > 0, "Error can't apply uninitialized species " +
        "template: " + species.name);

    MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
        world.GetScriptComponentHolder("MicrobeComponent").Find(microbe));

    MicrobeOperations::setMembraneType(world, microbe, species.membraneType);
    MicrobeOperations::setMembraneColour(world, microbe, species.colour);
    MicrobeOperations::setupMicrobeHitpoints(world, microbe,
        int(SimulationParameters::membraneRegistry().getTypeData(species.membraneType).hitpoints +
        microbeComponent.species.membraneRigidity * MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER));

    restoreOrganelleLayout(world, microbe, microbeComponent, species, editShape);

    // Another place where compound amounts are something we need to worry about
    auto ids = species.avgCompoundAmounts.getKeys();
    for(uint i = 0; i < ids.length(); i++){
        CompoundId compoundId = parseUInt(ids[i]);
        InitialCompound amount = InitialCompound(species.avgCompoundAmounts[ids[i]]);
        MicrobeOperations::setCompound(world, microbe, compoundId, amount.amount);
    }

}

void restoreOrganelleLayout(CellStageWorld@ world, ObjectID microbeEntity,
    MicrobeComponent@ microbeComponent, Species@ species,
    PhysicsShape@ editShape = null)
{
    // delete the the previous organelles.
    while(microbeComponent.organelles.length() > 0){

        assert(editShape is null,
            "can't directly edit a shape on a cell with existing organelles");

        // TODO: only ones that have been removed should be deleted

        auto organelle = microbeComponent.organelles[microbeComponent.organelles.length() - 1];
        auto q = organelle.q;
        auto r = organelle.r;
        // TODO: this could be done more efficiently
        MicrobeOperations::removeOrganelle(world, microbeEntity, {q, r});
    }

    // give it organelles
    for(uint i = 0; i < species.organelles.length(); i++){

        PlacedOrganelle@ organelle = PlacedOrganelle(
            cast<PlacedOrganelle>(species.organelles[i]));

        MicrobeOperations::addOrganelle(world, microbeEntity, organelle, editShape);
    }

    // Call this  to reset processor component
    MicrobeOperations::rebuildProcessList(world,microbeEntity);
}



//! Creates a species from an initial template
Species@ createSpecies(const string &in name, MicrobeTemplate@ fromTemplate)
{
    array<PlacedOrganelle@> convertedOrganelles;
    for(uint i = 0; i < fromTemplate.organelles.length(); i++){

        OrganelleTemplatePlaced@ organelle = fromTemplate.organelles[i];

        convertedOrganelles.insertLast(PlacedOrganelle(
                getOrganelleDefinition(organelle.type), organelle.q, organelle.r,
                organelle.rotation));
    }

    return createSpecies(name, fromTemplate.genus, fromTemplate.epithet, convertedOrganelles,
        fromTemplate.colour, fromTemplate.isBacteria, fromTemplate.membraneType, fromTemplate.membraneRigidity,
        fromTemplate.compounds, 100.0f, 100.0f, 100.0f, 200.0f, 100.0f);
}

//! Creates a Species object
//! \todo The behaviour traits should be put into a struct (aggression, fear etc.)
//! to reduce the number of parameters
Species@ createSpecies(const string &in name, const string &in genus,
    const string &in epithet, array<PlacedOrganelle@> organelles, Float4 colour,
    bool isBacteria, string membraneType, float membraneRigidity, const dictionary &in compounds,
    float aggression, float fear, float activity, float focus, float opportunism)
{
    Species@ species = Species(name);

    species.genus = genus;
    species.epithet = epithet;


    @species.avgCompoundAmounts = dictionary();

    @species.organelles = array<SpeciesStoredOrganelleType@>();
    species.stringCode = "";

    // Translate positions over
    for(uint i = 0; i < organelles.length(); ++i){
        auto organelle = cast<PlacedOrganelle>(organelles[i]);
        species.organelles.insertLast(organelle);
        species.stringCode += organelle.organelle.gene;
        // This will always be added after each organelle so its safe to assume its there
        species.stringCode+=","+organelle.q+","+
            organelle.r+","+
            organelle.rotation;
        if (i != organelles.length()-1){
            species.stringCode+="|";
        }
    }

    // Verify it //
    for(uint i = 0; i < species.organelles.length(); i++){

        PlacedOrganelle@ organelle = cast<PlacedOrganelle>(species.organelles[i]);

        if(organelle is null){

            assert(false, "createSpecies: species.organelles has invalid object at index: " +
                i);
        }
    }

    species.colour = colour;

    species.membraneType = SimulationParameters::membraneRegistry().getTypeId(membraneType);
    species.membraneRigidity = membraneRigidity;

    //We need to know this is baceria
    species.isBacteria = isBacteria;
    // We need to know our aggression and fear variables
    species.aggression = aggression;
    species.fear = fear;
    species.activity = activity;
    species.focus = focus;
    species.opportunism = opportunism;

    // Iterates over all compounds, and sets amounts and priorities
    uint64 compoundCount = SimulationParameters::compoundRegistry().getSize();
    for(uint i = 0; i < compoundCount; i++){

        auto compound = SimulationParameters::compoundRegistry().getTypeData(i);

        if(!compounds.exists(compound.internalName))
            continue;

        InitialCompound compoundAmount;
        if(!compounds.get(compound.internalName, compoundAmount)){

            assert(false, "createSpecies: invalid data in compounds, with key: " +
                compound.internalName);
            continue;
        }

        species.avgCompoundAmounts[formatUInt(compound.id)] = compoundAmount;
    }


    return species;
}

// Currently this goes through STARTER_MICROBES (defined in config.as)
// and creates species out of them
array<Species@> createDefaultSpecies()
{
    // Fail if compound registry is empty //
    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");

    auto keys = STARTER_MICROBES.getKeys();

    array<Species@> result;

    for(uint i = 0; i < keys.length(); ++i){

        const string name = keys[i];

        MicrobeTemplate@ data = cast<MicrobeTemplate@>(STARTER_MICROBES[name]);

        result.insertLast(Species::createSpecies(name, data));

        LOG_INFO("created starter microbe \"" + name + "\"");
    }

    LOG_INFO("setupSpecies created " + keys.length() + " species");
    return result;
}


}
