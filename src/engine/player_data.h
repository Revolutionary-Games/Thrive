#pragma once

#include "engine/typedefs.h"

#include <Entities/EntityCommon.h>

#include <memory>
#include <string>

namespace sol {
class state;
}

namespace thrive {

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
    PlayerData(std::string name);

    /**
     * @brief destructor
     */
    ~PlayerData();

    // /**
    // * @brief Lua bindings
    // *
    // * Exposes:
    // * - PlayerData::PlayerData
    // * - PlayerData::lockedMap
    // * - PlayerData::activeCreature
    // * - PlayerData::setActiveCreature
    // * - PlayerData::activeCreatureGamestate
    // * - PlayerData::isBoolSet
    // * - PlayerData::setBool
    // *
    // * @return
    // */
    // static void luaBindings(sol::state &lua);

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
    ObjectID
        activeCreature();

    /**
     * @brief setter for the players active creature
     *
     * @param creatureId
     *  Entity id of the creature
     *
     * @note If you call this make sure that the old player creature is dead
     *  or is now AI controlled
     */
    void
        setActiveCreature(ObjectID creatureId);

    /**
     * @brief Returns whether a key has a true bool set to it
     *
     * @return
     */
    bool
        isBoolSet(const std::string& key) const;

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
        setBool(const std::string& key, bool value);

    //! \returns True when the player is in freebuild mode and various things
    //! should be disabled / different
    bool
        isFreeBuilding() const;

    //! Enables freebuild. There is purposefully no way to undo this other than
    //! calling newGame
    void
        enterFreeBuild();

    //! \brief Resets the player data
    void
        newGame();

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


} // namespace thrive
