#pragma once

#include "engine/typedefs.h"

#include <string>
#include <memory>

namespace luabind {
    class scope;
}

namespace thrive {

class GameState;
class StorageContainer;
class LockedMap;

class PlayerData {

public:

    /**
    * @brief constructor
    *
    * @param name
    *  The name of the player
    */
    PlayerData(
        std::string name
    );

    /**
    * @brief destructor
    */
    ~PlayerData();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - PlayerData::PlayerData
    * - PlayerData::lockedMap
    * - PlayerData::activeCreature
    * - PlayerData::setActiveCreature
    * - PlayerData::activeCreatureGamestate
    * - PlayerData::isBoolSet
    * - PlayerData::setBool
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Getter for the players name
    *
    * @return
    */
    const std::string&
    playerName();

    /**
    * @brief Getter for the map of locked concepts
    *
    * @return
    */
    LockedMap&
    lockedMap();

    /**
    * @brief Getter for the id of the players currently active creature entity
    *
    * @return
    */
    EntityId
    activeCreature();

    /**
    * @brief setter for the players active creature
    *
    * @param creatureId
    *  Entity id of the creature
    *
    * @param gamestate
    *  The gamestate that the players new active creature belongs to
    */
    void
    setActiveCreature(
        EntityId creatureId,
        GameState& gamestate
    );

    /**
    * @brief Getter for the players currently active creatures gamestate
    *
    * @return
    */
    GameState&
    activeCreatureGamestate();

    /**
    * @brief Returns whether a key has a true bool set to it
    *
    * @return
    */
    bool
    isBoolSet(
        std::string key
    ) const ;

    /**
    * @brief Binds a string to a bool
    *
    * @param key
    *  The string key to bind
    *
    * @param value
    *  What value to bind the key to
    */
    void
    setBool(
        std::string key,
        bool value
    );

     /**
    * @brief Called by the engine during loading of a savegame
    *
    * @param storage
    *
    * @see PlayerData::storage()
    */
    void
    load(
        const StorageContainer& storage
    );

    /**
    * @brief Called by the engine during savegame creation
    *
    * @return
    *
    * @see PlayerData::load()
    */
    StorageContainer
    storage() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


}
