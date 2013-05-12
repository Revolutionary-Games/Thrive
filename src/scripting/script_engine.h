#pragma once

#include "engine/engine.h"

#include <memory>

class lua_State;

namespace thrive {

/**
* @brief Handles the initialization and execution of Lua scripts
*/
class ScriptEngine : public Engine {

public:

    /**
    * @brief Constructor
    *
    * @param L
    *   The Lua state to use
    */
    ScriptEngine(
        lua_State* L
    );

    /**
    * @brief Destructor
    */
    ~ScriptEngine();

    /**
    * @brief Initializes the engine
    *
    * This adds essential systems and loads all scripts
    *
    * @param entityManager
    *   The entity manager to use
    */
    void 
    init(
        EntityManager* entityManager
    ) override;

    /**
    * @brief The script engine's Lua state
    */
    lua_State*
    luaState();

    /**
    * @brief Shuts down the engine
    */
    void 
    shutdown() override;

    /**
    * @brief Renders a frame
    */
    void
    update() override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

