#pragma once


#include "biomes.h"
#include "bioprocesses.h"
#include "compounds.h"
#include "general/json_registry.h"
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
    // These are fully in AngelScript, though it would be nice to move the
    // parameters to json static TJsonRegistry<OrganelleType> organelleRegistry;

    static SpeciesNameController speciesNameController;

    static void
        init();
};

} // namespace thrive
