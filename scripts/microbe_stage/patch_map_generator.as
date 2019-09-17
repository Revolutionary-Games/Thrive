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

    Patch@ patch0 = Patch("Pangonian vents", 0, getBiomeTemplate("aavolcanic_vent"));
    auto defaultSpecies = Species::createDefaultSpecies();
    for(uint i = 0; i < defaultSpecies.length(); ++i){
        patch0.addSpecies(defaultSpecies[i]);
    }
    patch0.setScreenCoordinates(Float2(100,100));

    Patch@ patch1 = Patch("Pangonaian Mesopelagic", 1, getBiomeTemplate("mesopelagic"));
    patch1.setScreenCoordinates(Float2(200,100));
    patch1.addNeighbour(0);
    patch0.addNeighbour(1);

    Patch@ patch2 = Patch("Pangonian Epipelagic", 2, getBiomeTemplate("default"));
    patch2.setScreenCoordinates(Float2(200,200));
    patch2.addNeighbour(1);
    patch1.addNeighbour(2);

    map.addPatch(patch0);
    map.addPatch(patch1);
    map.addPatch(patch2);

    return map;
}

const Biome@ getBiomeTemplate(const string &in name)
{
    const auto id = SimulationParameters::biomeRegistry().getTypeId(name);
    return SimulationParameters::biomeRegistry().getTypeData(id);
}

}

