#include <iostream>
#include <cmath>
#include <algorithm>

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"

#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/process_system.h"

using namespace thrive;

REGISTER_COMPONENT(ProcessorComponent)

luabind::scope
ProcessorComponent::luaBindings() {
    using namespace luabind;
    return class_<ProcessorComponent, Component>("ProcessorComponent")
        .enum_("ID") [
            value("TYPE_ID", ProcessorComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &ProcessorComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("setThreshold", &ProcessorComponent::setThreshold)
        .def("setLowThreshold", &ProcessorComponent::setLowThreshold)
        .def("setHighThreshold", &ProcessorComponent::setHighThreshold)
        .def("setVentThreshold", &ProcessorComponent::setVentThreshold)
        .def("setCapacity", &ProcessorComponent::setCapacity)
    ;
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

luabind::scope
CompoundBagComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundBagComponent, Component>("CompoundBagComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundBagComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundBagComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("setProcessor", &CompoundBagComponent::setProcessor)
        .def("giveCompound", &CompoundBagComponent::giveCompound)
        .def("takeCompound", &CompoundBagComponent::takeCompound)
        .def("getCompoundAmount", &CompoundBagComponent::getCompoundAmount)
        .def("excessAmount", &CompoundBagComponent::excessAmount)
        .def("aboveLowThreshold", &CompoundBagComponent::aboveLowThreshold)
    ;
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
	this->processor = static_cast<ProcessorComponent*>(Entity(this->speciesName).getComponent(ProcessorComponent::TYPE_ID));
}

StorageContainer
CompoundBagComponent::storage() const
{
    StorageContainer storage = Component::storage();

    StorageContainer compounds;
    for (auto entry : this->compounds) {
        compounds.set<float>(""+entry.first, entry.second);
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

luabind::scope
ProcessSystem::luaBindings() {
    using namespace luabind;
    return class_<ProcessSystem, System>("ProcessSystem")
        .def(constructor<>())
    ;
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
ProcessSystem::init(GameState* gameState)
{
    System::initNamed("ProcessSystem", gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
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

void
ProcessSystem::Implementation::update(int logicTime) {
    //Iterating on each entity with a ProcessorComponent.
    for (auto& value : this->m_entities) {
        CompoundBagComponent* bag = std::get<0>(value.second);
        ProcessorComponent* processor = bag->processor;
        std::unordered_map<CompoundId, float> price;
        std::unordered_map<CompoundId, float> amount;

        //Phase one: setting up the prices.
        for (const auto& compound : bag->compounds) {
            CompoundId compoundId = compound.first;
            float compoundAmount = compound.second;
            price[compoundId] = 1 / (compoundAmount + 1);
        }

        //Phase two: setting up the processes.
        for (const auto& process : processor->process_capacities) {
            BioProcessId processId = process.first;
            float processCapacity = process.second;

            //The maximum capacity this process could have with the current amount of input compounds.
            float processLimitCapacity = processCapacity * logicTime;

            //Calculating the cost of the process's inputs.
            float cost = 0;
            for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                CompoundId inputId = input.first;
                int inputNeeded = input.second;
                cost += price[inputId] * inputNeeded;

                //Limiting the process by the amount of this required compound.
                processLimitCapacity = std::min(processLimitCapacity, bag->compounds[inputId] / inputNeeded);
            }

            //Calculating the revenue generated by the process's outputs.
            float revenue = 0;
            for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                CompoundId outputId = output.first;
                int outputGenerated = output.second;
                revenue += price[outputId] * outputGenerated;
            }

            //Setting the process capacity rate.
            float rate = 0;
            if(revenue > cost) rate = std::min(processCapacity * logicTime / 1000, processLimitCapacity);

            //Running the process at the specified rate, transforming the inputs...
            for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                CompoundId inputId = input.first;
                int inputNeeded = input.second;
                bag->compounds[inputId] -= rate * inputNeeded;
            }

            //...into the outputs.
            for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                CompoundId outputId = output.first;
                int outputGenerated = output.second;
                bag->compounds[outputId] += rate * outputGenerated;
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
