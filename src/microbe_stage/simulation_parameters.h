#pragma once


#include "biomes.h"
#include "bioprocesses.h"
#include "compounds.h"
#include "general/json_registry.h"
#include "species.h"
#include "species_name_controller.h"

#include <unordered_map>

namespace thrive {

class BioProcess;
class Biome;

class SimulationParameters {
public:
    static TJsonRegistry<Compound> compoundRegistry;
    static TJsonRegistry<BioProcess> bioProcessRegistry;
    static TJsonRegistry<Biome> biomeRegistry;
    // These are fully in AngelScript
    // static TJsonRegistry<OrganelleType> organelleRegistry;
    static TJsonRegistry<Species> speciesRegistry;

    static SpeciesNameController speciesNameController;

    static std::unordered_map<size_t, unsigned int> newSpeciesStartingCompounds;

    static void
        init();
};

} // namespace thrive
