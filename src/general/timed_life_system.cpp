#include "general/timed_life_system.h"

#include <Entities/GameWorld.h>

using namespace thrive;

TimedLifeComponent::TimedLifeComponent(float timeToLive) :
    Leviathan::Component(TYPE), m_timeToLive(timeToLive)
{}

// void
// TimedLifeComponent::load(
//     const StorageContainer& storage
// ) {
//     Component::load(storage);
//     m_timeToLive = storage.get<Milliseconds>("timeToLive");
// }


// StorageContainer
// TimedLifeComponent::storage() const {
//     StorageContainer storage = Component::storage();
//     storage.set<Milliseconds>("timeToLive", m_timeToLive);
//     return storage;
// }


////////////////////////////////////////////////////////////////////////////////
// TimedLifeSystem
////////////////////////////////////////////////////////////////////////////////

void
    TimedLifeSystem::Run(GameWorld& world,
        std::unordered_map<ObjectID, TimedLifeComponent*>& components,
        float elapsed)
{
    for(auto& value : components) {
        TimedLifeComponent* timedLifeComponent = value.second;
        timedLifeComponent->m_timeToLive -= elapsed;
        if(timedLifeComponent->m_timeToLive <= 0) {

            world.QueueDestroyEntity(value.first);
        }
    }
}
