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
            // Loop through all the compounds in the storage bag and eject them
            bool vented = false;
            for(const auto& compound : bag.compounds) {
                double compoundAmount = compound.second.amount;
                CompoundId compoundId = compound.first;
                if(venter.ventAmount <= compoundAmount) {
                    Leviathan::Position& position = std::get<2>(*value.second);
                    venter.ventCompound(
                        position, compoundId, venter.ventAmount, world);
                    bag.takeCompound(compoundId, venter.ventAmount);
                    vented = true;
                }
            }

            // If you did not vent anything this step and the venter component
            // is flagged to dissolve tyou, dissolve you
            if(vented == false && venter.getDoDissolve()) {
                world.QueueDestroyEntity(value.first);
            }
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

void
    CompoundVenterComponent::setDoDissolve(bool dissolve)
{
    this->doDissolve = dissolve;
}

bool
    CompoundVenterComponent::getDoDissolve()
{
    return this->doDissolve;
}