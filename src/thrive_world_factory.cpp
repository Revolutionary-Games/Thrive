// ------------------------------------ //
#include "thrive_world_factory.h"

#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"

using namespace thrive;
// ------------------------------------ //
ThriveWorldFactory::ThriveWorldFactory()
{
    StaticInstance = this;
}

ThriveWorldFactory::~ThriveWorldFactory() {}

std::shared_ptr<Leviathan::GameWorld>
    ThriveWorldFactory::CreateNewWorld(int worldtype,
        const std::shared_ptr<Leviathan::PhysicsMaterialManager>&
            physicsMaterials,
        int overrideid)
{
    THRIVE_WORLD_TYPE castedType = static_cast<THRIVE_WORLD_TYPE>(worldtype);
    switch(castedType) {
    case THRIVE_WORLD_TYPE::CELL_STAGE:
        return std::make_shared<CellStageWorld>(physicsMaterials, overrideid);
    case THRIVE_WORLD_TYPE::MICROBE_EDITOR:
        return std::make_shared<MicrobeEditorWorld>(
            physicsMaterials, overrideid);
    default:
        LEVIATHAN_ASSERT(false,
            "ThriveWorldFactory unknown type: " + std::to_string(worldtype));
        throw std::runtime_error("unknown world type");
    }
}
