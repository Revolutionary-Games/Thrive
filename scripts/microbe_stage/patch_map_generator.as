// The patch map generator is defined in this script file to make modifying it easier
PatchMap@ generatePatchMap()
{
    return PatchMapGenerator::runGeneration();
}

// Private hidden functions
namespace PatchMapGenerator{

PatchMap@ runGeneration()
{
    PatchMap@ map = PatchMap();

    Patch@ patch1 = Patch("Pangonian vents", 0, getBiomeTemplate("Volcanic vent"));

    auto defaultSpecies = Species::createDefaultSpecies();

    for(uint i = 0; i <= defaultSpecies.length(); ++i){
        patch1.addSpecies(defaultSpecies[i]);
    }

    map.addPatch(patch1);

    return map;
}

const Biome@ getBiomeTemplate(const string &in name)
{
    const auto id = SimulationParameters::biomeRegistry().getTypeId(name);
    return SimulationParameters::biomeRegistry().getTypeData(id);
}

}

