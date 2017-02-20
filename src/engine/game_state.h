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

//class CEGUIWindow;
//class Engine;
class EntityManager;
//class StorageContainer;
class System;

class PhysicalWorld;


/**
* @brief Wrapper that allows C++ systems to access Lua engine data
*
* @note Some of these methods might be really slow so avoid if possible.
* Or maybe rewrite the system in Lua.
*/
class LuaEngine {
public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - LuaEngine
    *
    * @return
    */
    static void luaBindings(sol::state &lua);
    
    LuaEngine(sol::table engineObj);


    bool isSystemTimedShutdown(System& system);

    void timedSystemShutdown(System& system, int tineInMS);
};


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
    static void luaBindings(sol::state &lua);


    GameStateData(sol::table stateObj, LuaEngine* luaEngine);

    ~GameStateData();

    LuaEngine*
        luaEngine();

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
        S* findSystem()
    {
        for (const auto& system : this->getCppSystems()) {
            S* foundSystem = dynamic_cast<S*>(system);
            if (foundSystem) {
                return foundSystem;
            }
        }
        return nullptr;
    }

    std::vector<System*> getCppSystems();

private:

    LuaEngine* m_engine;
    Ogre::SceneManager* m_sceneManager = nullptr;
};

}
