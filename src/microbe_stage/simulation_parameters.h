#pragma once

#include "backgrounds.h"
#include "biomes.h"
#include "bioprocesses.h"
#include "compounds.h"
#include "organelle_types.h"
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
    static TJsonRegistry<Background> backgroundRegistry;
    static TJsonRegistry<OrganelleType> organelleRegistry;

    static SpeciesNameController speciesNameController;

    static void
        init();
};

} // namespace thrive
