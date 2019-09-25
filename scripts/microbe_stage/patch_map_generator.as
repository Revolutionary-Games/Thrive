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
    patch0.setScreenCoordinates(Float2(100, 400));
    patch0.addNeighbour(10);

    Patch@ patch1 = Patch("Pangonian Mesopelagic", 1, getBiomeTemplate("mesopelagic"));
    patch1.setScreenCoordinates(Float2(200, 200));
    patch1.addNeighbour(4);
    patch1.addNeighbour(2);
    patch1.addNeighbour(8);

    Patch@ patch2 = Patch("Pangonian Epipelagic", 2, getBiomeTemplate("default"));
    patch2.setScreenCoordinates(Float2(200, 100));
    patch2.addNeighbour(1);
    patch2.addNeighbour(3);
    patch2.addNeighbour(6);
    patch2.addNeighbour(9);

    Patch@ patch3 = Patch("Pangonian Tidepool", 3, getBiomeTemplate("tidepool"));
    patch3.setScreenCoordinates(Float2(300, 100));
    patch3.addNeighbour(2);

    Patch@ patch4 = Patch("Pangonian Bathypalagic", 4, getBiomeTemplate("bathypalagic"));
    patch4.setScreenCoordinates(Float2(200, 300));
    patch4.addNeighbour(5);
    patch4.addNeighbour(1);
    patch4.addNeighbour(10);

    Patch@ patch5 = Patch("Pangonian Abyssopelagic", 5, getBiomeTemplate("abyssopelagic"));
    patch5.setScreenCoordinates(Float2(300, 400));
    patch5.addNeighbour(10);
    patch5.addNeighbour(4);


    Patch@ patch6 = Patch("Pangonian Coast", 6, getBiomeTemplate("coastal"));
    patch6.setScreenCoordinates(Float2(100, 100));
    patch6.addNeighbour(2);
    patch6.addNeighbour(7);

    Patch@ patch7 = Patch("Pangonian Estuary", 7, getBiomeTemplate("estuary"));
    patch7.setScreenCoordinates(Float2(70, 160));
    patch7.addNeighbour(6);

    Patch@ patch8 = Patch("Cave", 8, getBiomeTemplate("underwater_cave"));
    patch8.setScreenCoordinates(Float2(300, 200));
    patch8.addNeighbour(1);

    Patch@ patch9 = Patch("Ice Shelf", 9, getBiomeTemplate("ice_shelf"));
    patch9.setScreenCoordinates(Float2(200, 30));
    patch9.addNeighbour(2);


    Patch@ patch10 = Patch("Pangonian Sea Floor", 10, getBiomeTemplate("seafloor"));
    patch10.setScreenCoordinates(Float2(200, 400));
    patch10.addNeighbour(4);
    patch10.addNeighbour(5);
    patch10.addNeighbour(0);


    map.addPatch(patch0);
    map.addPatch(patch1);
    map.addPatch(patch2);
    map.addPatch(patch3);
    map.addPatch(patch4);
    map.addPatch(patch5);
    map.addPatch(patch6);
    map.addPatch(patch7);
    map.addPatch(patch8);
    map.addPatch(patch9);
    map.addPatch(patch10);

    return map;
}

const Biome@ getBiomeTemplate(const string &in name)
{
    const auto id = SimulationParameters::biomeRegistry().getTypeId(name);
    return SimulationParameters::biomeRegistry().getTypeData(id);
}

}

