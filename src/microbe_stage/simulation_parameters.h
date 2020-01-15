#pragma once

#include "backgrounds.h"
#include "biomes.h"
#include "bioprocesses.h"
#include "compounds.h"
#include "general/json_registry.h"
#include "membrane_types.h"
#include "organelle_types.h"
#include "species_name_controller.h"

namespace thrive {

class BioProcess;
class Biome;

//! \brief Handles loading parameters from json files
class SimulationParameters {
public:
    static TJsonRegistry<Compound> compoundRegistry;
    static TJsonRegistry<BioProcess> bioProcessRegistry;
    static TJsonRegistry<Biome> biomeRegistry;
    static TJsonRegistry<Background> backgroundRegistry;
    static TJsonRegistry<OrganelleType> organelleRegistry;
    static TJsonRegistry<MembraneType> membraneRegistry;

    static SpeciesNameController speciesNameController;

    static void
        init();
};

} // namespace thrive
