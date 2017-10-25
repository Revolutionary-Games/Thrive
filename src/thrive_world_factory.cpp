// ------------------------------------ //
#include "thrive_world_factory.h"

#include "generated/cell_stage_world.h"

using namespace thrive;
// ------------------------------------ //
ThriveWorldFactory::ThriveWorldFactory(){

    StaticInstance = this;
}

ThriveWorldFactory::~ThriveWorldFactory(){

}

std::shared_ptr<Leviathan::GameWorld> ThriveWorldFactory::CreateNewWorld(){

    return std::make_shared<CellStageWorld>();
}

