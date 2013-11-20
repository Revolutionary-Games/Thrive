#include "collision_filter.h"
#include <luabind/iterator_policy.hpp>
#include "engine/game_state.h"
#include "scripting/luabind.h"



using namespace thrive;

struct CollisionFilter::Implementation {

    Implementation(
        const std::string& collisionGroup1,
        const std::string& collisionGroup2
    ) : m_signature(collisionGroup1, collisionGroup2)
    {
    }

    CollisionMap m_collisions;

    Signature m_signature;

    CollisionSystem* m_collisionSystem = nullptr;

};


luabind::scope
CollisionFilter::luaBindings() {
    using namespace luabind;
    return class_<CollisionFilter>("CollisionFilter")
        .def(constructor<const std::string&, const std::string&>())
        .def("init", &CollisionFilter::init)
        .def("shutdown", &CollisionFilter::shutdown)
        .def("collisions", &CollisionFilter::collisions, return_stl_iterator)
        .def("clearCollisions", &CollisionFilter::clearCollisions)
    ;
}


CollisionFilter::CollisionFilter(
    const std::string& collisionGroup1,
    const std::string& collisionGroup2
) : m_impl(new Implementation(collisionGroup1, collisionGroup2))
{
}

CollisionFilter::~CollisionFilter(){}

void
CollisionFilter::init(
    GameState* gameState
) {
    m_impl->m_collisionSystem = gameState->findSystem<CollisionSystem>();
    m_impl->m_collisionSystem->registerCollisionFilter(*this);
}

void
CollisionFilter::shutdown() {
    m_impl->m_collisionSystem->unregisterCollisionFilter(*this);
    m_impl->m_collisionSystem = nullptr;
}

const CollisionFilter::CollisionIterator
CollisionFilter::collisions() {
    return m_impl->m_collisions | boost::adaptors::map_values;
}


void
CollisionFilter::addCollision(
    Collision collision
) {
    CollisionMap::iterator foundCollision = m_impl->m_collisions.find(CollisionId(collision.entityId1, collision.entityId2));
    if (foundCollision != m_impl->m_collisions.end())
         foundCollision->second.addedCollisionDuration += collision.addedCollisionDuration; //Add collision time.
    else
    {
        CollisionId key(collision.entityId1, collision.entityId2);
        m_impl->m_collisions.emplace(key, collision);
    }

}


typename CollisionFilter::CollisionIterator::iterator
CollisionFilter::begin() const {
    return (m_impl->m_collisions | boost::adaptors::map_values).begin();
}


typename CollisionFilter::CollisionIterator::iterator
CollisionFilter::end() const {
    return (m_impl->m_collisions | boost::adaptors::map_values).end();
}


void
CollisionFilter::clearCollisions() {
    m_impl->m_collisions.clear();
}


const CollisionFilter::Signature&
CollisionFilter::getCollisionSignature() const {
    return m_impl->m_signature;
}


size_t
CollisionFilter::IdHash::operator() (
    const CollisionId& collisionId
) const {
    std::size_t hash1 = std::hash<EntityId>()(collisionId.first);
    std::size_t hash2 = std::hash<EntityId>()(collisionId.second);
    // Hash needs to be symmetric so that hash(entityId1, entityId2) == hash(entityId2, entityId1)
    return hash1 ^ hash2;
}


bool
CollisionFilter::IdEquals::operator() (
    const CollisionId& lhs,
    const CollisionId& rhs
) const{
    // Equality needs to be symmetric for collisions
    return (lhs.first == rhs.first && lhs.second == rhs.second) ||
           (lhs.first == rhs.second && lhs.second == rhs.first);
}
