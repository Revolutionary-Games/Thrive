#include <algorithm>
#include <cmath>
#include <iostream>
#include <map>

#include "engine/serialization.h"

#include "general/thrive_math.h"
#include "simulation_parameters.h"

#include "microbe_stage/compound_venter_system.h"
#include "microbe_stage/process_system.h"

#include "generated/cell_stage_world.h"
#include <Entities/GameWorld.h>

using namespace thrive;

void
    CompoundVenterSystem::Run(CellStageWorld& world)
{
    if(!world.GetNetworkSettings().IsAuthoritative)
        return;

    for(auto& value : CachedComponents.GetIndex()) {

        CompoundBagComponent& bag = std::get<0>(*value.second);
        CompoundVenterComponent& venter = std::get<1>(*value.second);
        Leviathan::Position& position = std::get<2>(*value.second);

        venter.ventCompound(position,
            SimulationParameters::compoundRegistry.getTypeId("iron"), world);
    }
}

void
    CompoundVenterComponent::ventCompound(Leviathan::Position& pos,
        CompoundId compound,
        CellStageWorld& world)
{
    world.GetCompoundCloudSystem().addCloud(
        compound, 15000, pos.Members._Position);
}