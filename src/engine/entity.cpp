#include "engine/entity.h"

#include <atomic>

using namespace thrive;

const Entity::Id Entity::NULL_ID = 0;

Entity::Id
Entity::generateNewId() {
    static std::atomic<Entity::Id> currentId(Entity::NULL_ID);
    return currentId.fetch_add(1);
}
