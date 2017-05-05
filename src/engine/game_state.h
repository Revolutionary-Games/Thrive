#pragma once

#include <memory>

#include "scripting/luajit.h"


namespace sol {
class state;
}

namespace Ogre {
class SceneManager;
}

namespace thrive {

class Engine;
class EntityManager;
class System;

class PhysicalWorld;

/**
* @brief Wrapper that allows C++ systems to access state data owned by
* Lua also owns the states Ogre::SceneManager because that was easier
* to setup in C++ than in lua
*
* @note Some of these methods might be really slow so avoid if possible.
* Or maybe rewrite the system in Lua.
*/
class GameStateData {
public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - GameStateData
    *
    * @return
    */
    static void
    luaBindings(sol::state &lua);


    GameStateData(
        sol::table stateObj,
        Engine* engine,
        EntityManager* entityManager,
        PhysicalWorld* physics
    );

    ~GameStateData();

    Engine*
    engine();

    EntityManager*
    entityManager();

    PhysicalWorld*
    physicalWorld();

    /**
    * @brief The Ogre scene manager
    */
    Ogre::SceneManager*
    sceneManager() const;

    /**
    * @brief Finds a C++ based system matching type S
    */
    template<typename S>
    S* findSystem(){
        
        for (const auto& system : this->getCppSystems()) {
            S* foundSystem = dynamic_cast<S*>(system);
            if (foundSystem) {
                return foundSystem;
            }
        }
        return nullptr;
    }

    /**
    * @brief retrieves C++ systems from the lua state
    *
    * This is pretty slow so avoid if possible
    */
    std::vector<System*>
    getCppSystems();

    /*
    * @brief Returns the name of this game state
    */
    std::string
    name() const;

private:

    Engine* m_engine;
    EntityManager* m_entityManager;
    PhysicalWorld* m_physicalWorld;
    Ogre::SceneManager* m_sceneManager = nullptr;

    sol::table m_luaSide;
};

}
