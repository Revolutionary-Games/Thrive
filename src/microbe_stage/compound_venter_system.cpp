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


// ------------------------------------ //
// CompoundVenterComponent
CompoundVenterComponent::CompoundVenterComponent() : Leviathan::Component(TYPE)
{}

void
    CompoundVenterSystem::Run(CellStageWorld& world)
{
    if(!world.GetNetworkSettings().IsAuthoritative)
        return;

    const auto logicTime = Leviathan::TICKSPEED;

    timeSinceLastCycle++;
    while(timeSinceLastCycle > TIME_SCALING_FACTOR) {
        timeSinceLastCycle -= TIME_SCALING_FACTOR;
        for(auto& value : CachedComponents.GetIndex()) {
            CompoundBagComponent& bag = std::get<0>(*value.second);
            CompoundVenterComponent& venter = std::get<1>(*value.second);
            Leviathan::Position& position = std::get<2>(*value.second);
            venter.ventCompound(position,
                SimulationParameters::compoundRegistry.getTypeId("iron"),
                venter.ventAmount, world);
        }
    }
}

void
    CompoundVenterComponent::ventCompound(Leviathan::Position& pos,
        CompoundId compound,
        double amount,
        CellStageWorld& world)
{
    world.GetCompoundCloudSystem().addCloud(
        compound, amount * 1000.0f, pos.Members._Position);
}

void
    CompoundVenterComponent::setVentAmount(float amount)
{
    this->ventAmount = amount;
}

float
    CompoundVenterComponent::getVentAmount()
{
    return this->ventAmount;
}