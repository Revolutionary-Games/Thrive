#pragma once

#include <boost/range/adaptor/map.hpp>

#include "bullet/collision_system.h"

#include <iostream>
#include <string>

#include <unordered_set>
#include <utility>


namespace luabind {
class scope;
}

namespace thrive {

class Collision;
class CollisionSystem;

/**
* @brief Filters for collisions that contain specific collision groups
*
* Collision filter makes it easy for systems and other peices of code to get easy
*  access to the right collisions
*
*/

class CollisionFilter {

public:

    using CollisionId = std::pair<EntityId, EntityId>;

    using Signature = std::pair<std::string, std::string>;

    struct IdHash {
        std::size_t
        operator() (
            const CollisionId& collisionId
        ) const;
    };

    struct IdEquals {
        bool
        operator() (
            const CollisionId& lhs,
            const CollisionId& rhs
        ) const;
    };

    using CollisionMap = std::unordered_map<CollisionId, Collision, IdHash, IdEquals>;
    using CollisionIterator = boost::range_detail::select_second_mutable_range<CollisionMap>;

    /**
    * @brief Constructor
    *
    * @param collisionGroup1
    *   The first collision group to monitor
    *
    * @param collisionGroup2
    *   The second collision group to monitor
    *
    */
    CollisionFilter(
        const std::string& collisionGroup1,
        const std::string& collisionGroup2
    );

    /**
    * @brief Destructor
    */
    ~CollisionFilter();

    /**
    * @brief Initialized the collision filter
    *
    * @param gameState
    *  The gamestate the filter belongs in.
    */
    void
    init(
        GameState* gameState
    );

    /**
    * @brief Shuts down the filter.
    */
    void
    shutdown();

    /**
    * @brief Lua bindings
    *
    * Exposes the following \b constructors:
    * - CollisionFilter(const std::string&, const std::string&)
    * - CollisionFilter::init(GameState*)
    * - CollisionFilter::shutdown()
    * - CollisionFilter::collisions()
    * - CollisionFilter::clearCollisions()
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Returns the collisions that has occoured
    *
    * Is only reset when clearCollisions() is called
    */
    const CollisionIterator
    collisions();

    /**
    * @brief Clears the collisions
    */
    void
    clearCollisions();

    /**
    * @brief Adds a collision
    *
    * @param collision
    *   Collision to add
    */
    void
    addCollision(Collision collision);

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * collisions().cbegin()
    * \endcode
    *
    * @return An iterator to the first collision
    */
    typename CollisionIterator::iterator
    begin() const;

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * collisions().cend()
    * \endcode
    *
    * @return An iterator to the end of the collisions
    */
    typename CollisionIterator::iterator
    end() const;

    /**
    * @brief Returns the signature of the collision filter
    *
    * @return
    *   A pair of the two collision group strings.
    */
    const Signature&
    getCollisionSignature() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
