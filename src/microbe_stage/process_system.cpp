#include <iostream>
#include <cmath>

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"

#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/process_system.h"

using namespace thrive;

REGISTER_COMPONENT(ProcessorComponent)

void ProcessorComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<ProcessorComponent>("ProcessorComponent",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<Component>(),

        "TYPE_ID", sol::var(ProcessorComponent::TYPE_ID), 
        "TYPE_NAME", &ProcessorComponent::TYPE_NAME,

        "setThreshold", &ProcessorComponent::setThreshold,
        "setLowThreshold", &ProcessorComponent::setLowThreshold,
        "setHighThreshold", &ProcessorComponent::setHighThreshold,
        "setVentThreshold", &ProcessorComponent::setVentThreshold,
        "setCapacity", &ProcessorComponent::setCapacity
    );
}

void
ProcessorComponent::load(const StorageContainer& storage)
{
    Component::load(storage);

    StorageContainer lua_thresholds = storage.get<StorageContainer>("thresholds");
    for (const std::string& id : lua_thresholds.keys())
    {
        StorageContainer threshold = lua_thresholds.get<StorageContainer>(id);
        float low = threshold.get<float>("low");
        float high = threshold.get<float>("high");
        float vent = threshold.get<float>("vent");
		this->thresholds[std::atoi(id.c_str())] = std::tuple<float, float, float>(low, high, vent);
	}

    StorageContainer processes = storage.get<StorageContainer>("processes");
    for (const std::string& id : processes.keys())
    {
        this->process_capacities[std::atoi(id.c_str())] = processes.get<float>(id);
	}
}

StorageContainer
ProcessorComponent::storage() const
{
	StorageContainer storage = Component::storage();

	StorageContainer lua_thresholds;
	for (auto entry : this->thresholds) {
        StorageContainer threshold;
        threshold.set<float>("low", std::get<0>(entry.second));
        threshold.set<float>("high", std::get<1>(entry.second));
        threshold.set<float>("vent", std::get<2>(entry.second));
        lua_thresholds.set<StorageContainer>(std::to_string(static_cast<int>(entry.first)), threshold);
	}
    storage.set<StorageContainer>("thresholds", lua_thresholds);

	StorageContainer processes;
    for (auto entry : this->process_capacities) {
        processes.set<float>(std::to_string(static_cast<int>(entry.first)), entry.second);
    }
    storage.set<StorageContainer>("processes", processes);


	return storage;
}

void
ProcessorComponent::setThreshold(CompoundId id, float low, float high, float vent)
{
    this->thresholds[id] = std::tuple<float, float, float>(low, high, vent);
}

void
ProcessorComponent::setLowThreshold(CompoundId id, float low)
{
    std::get<0>(this->thresholds[id]) = low;
}

void
ProcessorComponent::setHighThreshold(CompoundId id, float high)
{
    std::get<1>(this->thresholds[id]) = high;
}

void
ProcessorComponent::setVentThreshold(CompoundId id, float vent)
{
    std::get<2>(this->thresholds[id]) = vent;
}

void
ProcessorComponent::setCapacity(BioProcessId id, float capacity)
{
    this->process_capacities[id] = capacity;
}

REGISTER_COMPONENT(CompoundBagComponent)

void CompoundBagComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CompoundBagComponent>("CompoundBagComponent",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<Component>(),

        "TYPE_ID", sol::var(CompoundBagComponent::TYPE_ID), 
        "TYPE_NAME", &CompoundBagComponent::TYPE_NAME,

        "setProcessor", &CompoundBagComponent::setProcessor,
        "giveCompound", &CompoundBagComponent::giveCompound,
        "takeCompound", &CompoundBagComponent::takeCompound,
        "getCompoundAmount", &CompoundBagComponent::getCompoundAmount,
        "excessAmount", &CompoundBagComponent::excessAmount,
        "aboveLowThreshold", &CompoundBagComponent::aboveLowThreshold
    );
}

CompoundBagComponent::CompoundBagComponent() {
    for (CompoundId id : CompoundRegistry::getCompoundList()) {
        compounds[id] = 0;
    }
}

void
CompoundBagComponent::load(const StorageContainer& storage)
{
    Component::load(storage);

    StorageContainer compounds = storage.get<StorageContainer>("compounds");

    for (const std::string& id : compounds.keys())
    {
        this->compounds[std::atoi(id.c_str())] = compounds.get<float>(id);
	}

	this->speciesName = storage.get<std::string>("speciesName");
	this->processor = static_cast<ProcessorComponent*>(Entity(this->speciesName,
            Game::instance().engine().getCurrentGameStateFromLua()).
        getComponent(ProcessorComponent::TYPE_ID));
}

StorageContainer
CompoundBagComponent::storage() const
{
    StorageContainer storage = Component::storage();

    StorageContainer compounds;
    for (auto entry : this->compounds) {
        compounds.set<float>(""+std::to_string(static_cast<int64_t>(entry.first)),
            entry.second);
    }
    storage.set("compounds", std::move(compounds));

    storage.set("speciesName", this->speciesName);

    return storage;
}

void
CompoundBagComponent::setProcessor(ProcessorComponent& processor, const std::string& speciesName) {
    this->processor = &processor;
    this->speciesName = speciesName;
}

// helper methods for integrating compound bags with current, un-refactored, lua microbes
float
CompoundBagComponent::getCompoundAmount(CompoundId id) {
    return compounds[id];
}

void
CompoundBagComponent::giveCompound(CompoundId id, float amt) {
    compounds[id] += amt;
}

float
CompoundBagComponent::takeCompound(CompoundId id, float to_take) {
    float& ref = compounds[id];
    float amt = ref > to_take ? to_take : ref;
    ref -= amt;
    return amt;
}

float
CompoundBagComponent::excessAmount(CompoundId id) {
    float amt = compounds[id];
    float threshold = std::get<2>(this->processor->thresholds[id]);
    return amt > threshold ? amt - threshold : 0;
}

float
CompoundBagComponent::aboveLowThreshold(CompoundId id) {
    float amt = compounds[id];
    float threshold = std::get<0>(this->processor->thresholds[id]);
    return amt > threshold ? amt - threshold : 0;
}

void ProcessSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<ProcessSystem>("ProcessSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>(),

        "init", &ProcessSystem::init
    );
}


struct ProcessSystem::Implementation {

    EntityFilter<
        CompoundBagComponent
    > m_entities;

    void update(int);
    void updateAddedEntites(int);
    void updateRemovedEntities(int);
    inline float step_function(float, float, float, float);
    inline float step_2(float, float, float);

    static constexpr float SMOOTHING_FACTOR = 1.8;
    static constexpr float TIME_SCALING_FACTOR = 1000;
};

ProcessSystem::ProcessSystem()
    : m_impl(new Implementation())
{

}

ProcessSystem::~ProcessSystem()
{

}

void
ProcessSystem::init(GameStateData* gameState)
{
    System::initNamed("ProcessSystem", gameState);
    m_impl->m_entities.setEntityManager(gameState->entityManager());
}

void
ProcessSystem::shutdown()
{

}

void
ProcessSystem::Implementation::updateRemovedEntities(int) {
    // std::cerr << logicTime;
    // for (EntityId entityId : this->m_entities.removedEntities()) {
        // std::cerr << &entityId;
    // }
}

void
ProcessSystem::Implementation::updateAddedEntites(int) {
    // std::cerr << logicTime;
    // for (auto& value : this->m_entities.addedEntities()) {
        // std::cerr << &value;
    // }
}

/*
#create a step function
#return positive if below low, negative if above high
def step_function(value, threshold, high_threshold, vent_threshold):
    if value >= high_threshold:
        return -float(value - high_threshold)/(vent_threshold - high_threshold)
    elif value >= threshold:
        return 0
    elif value < threshold and threshold != 0 and value >= 0:
        return 1 - (float(value)/threshold)
    else:
        print "error in step function, I was passed a negative value"
        return 0

*/


// 0 <= value <=> threshold < high_threshold < vent_threshold
inline float
ProcessSystem::Implementation::step_function(float value, float threshold, float high_threshold, float vent_threshold) {
    if (value >= high_threshold) {
        return (high_threshold - value) / (vent_threshold - high_threshold);
    }
    if (value >= threshold) {
        return 0;
    }
    return 1 - (value/threshold);
}

inline float
ProcessSystem::Implementation::step_2(float value, float threshold, float high_threshold) {
    if (value > high_threshold)
        return high_threshold - value;
    if (value >= threshold) {
        return 0;
    }
    return 1 - value/threshold;
}

void
ProcessSystem::Implementation::update(int) { // int is logicTime
    // std::cerr << logicTime;
    for (auto& value : this->m_entities) {
        CompoundBagComponent* bag = std::get<0>(value.second);
        ProcessorComponent* processor = bag->processor;
        std::unordered_map<CompoundId, float> actions;
        for (const auto& compound : bag->compounds) {
            actions[compound.first] = step_2(compound.second, std::get<0>(processor->thresholds[compound.first]), std::get<1>(processor->thresholds[compound.first]));
        }
        for (const auto& process : processor->process_capacities) {
            if (process.second <= 0) continue;
            float input_rate = -1, output_rate = -1;
            for (const auto& input : BioProcessRegistry::getInputCompounds(process.first)) {
                const float p = actions[input.first];
                input_rate = input_rate >= p ? input_rate : p;
            }
            for (const auto& output : BioProcessRegistry::getOutputCompounds(process.first)) {
                const float p = actions[output.first];
                output_rate = output_rate >= p ? output_rate : p;
            }

            float rate = output_rate - input_rate;
            if (rate > 0) {
                // scale down the rate using the process's bandwidth and smoothing factor

                rate = process.second * (1 - exp(-rate * SMOOTHING_FACTOR/process.second));

                bool will_run = true;
                // can we guarantee that will_run will never be set to false unless there's a bug?
                // I think so
                for (const auto& input : BioProcessRegistry::getInputCompounds(process.first)) {
                    if (rate * input.second >= bag->compounds[input.first]) {
                        will_run = false;
                        break;
                    }
                }
                if (will_run) {
                    for (const auto& input : BioProcessRegistry::getInputCompounds(process.first)) {
                        bag->compounds[input.first] -= rate * input.second;
                    }
                    for (const auto& output : BioProcessRegistry::getOutputCompounds(process.first)) {
                        bag->compounds[output.first] += rate * output.second;
                    }
                }
            }
        }
    }
}

void
ProcessSystem::update(int, int logicTime)
{
    m_impl->updateRemovedEntities(logicTime);
    m_impl->updateAddedEntites(logicTime);
    m_impl->m_entities.clearChanges();
    m_impl->update(logicTime);
}
