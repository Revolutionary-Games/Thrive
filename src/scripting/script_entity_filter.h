#pragma once

#include "engine/typedefs.h"
#include "scripting/luabind.h"

#include <luabind/object.hpp>
#include <memory>
#include <unordered_set>

namespace thrive {

class GameState;

/**
* @brief Script version of the EntityFilter
*/
class ScriptEntityFilter {

public:

    /**
    * @brief Lua bindings
    *
    * - ScriptEntityFilter::addedEntities
    * - ScriptEntityFilter::clearChanges
    * - ScriptEntityFilter::containsEntity
    * - ScriptEntityFilter::entities
    * - ScriptEntityFilter::init
    * - ScriptEntityFilter::removedEntities
    * - ScriptEntityFilter::shutdown
    *
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * @param componentTypes
    *   A Lua table containing the Lua class objects of the components
    *   that should be filtered for
    * @param recordChanges
    *   If \c true, this entity filter will keep track of the entities added
    *   and removed during a frame.
    */
    ScriptEntityFilter(
        luabind::object componentTypes,
        bool recordChanges
    );

    /**
    * @brief Constructor
    *
    * This is the same as the other constructor, but with \a recordChanges as
    * \c false.
    *
    * @param componentTypes
    *   A Lua table containing the Lua class objects of the components
    *   that should be filtered for
    */
    ScriptEntityFilter(
        luabind::object componentTypes
    );

    /**
    * @brief Destructor
    */
    ~ScriptEntityFilter();

    /**
    * @brief Returns the set of added entities
    *
    * Be sure to call clearChanges() once you have processed all added and
    * removed entities.
    *
    */
    const std::unordered_set<EntityId>&
    addedEntities();

    /**
    * @brief Clears added and removed entities from this filter
    */
    void
    clearChanges();

    /**
    * @brief Checks for an entity id
    *
    * @param id
    *   The id to check for
    *
    * @return 
    *   \c true if the entity is contained in this filter, \c false otherwise.
    */
    bool
    containsEntity(
        EntityId id
    ) const;

    /**
    * @brief The set of entities that are contained in this filter
    *
    */
    const std::unordered_set<EntityId>&
    entities();

    /**
    * @brief Initializes this filter
    */
    void
    init(
        GameState* gameState
    );

    /**
    * @brief The set of removed entities
    *
    * Be sure to call clearChanges() once you have processed all added and
    * removed entities.
    */
    const std::unordered_set<EntityId>&
    removedEntities();

    /**
    * @brief Shuts this filter down
    */
    void
    shutdown();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    

};

}
