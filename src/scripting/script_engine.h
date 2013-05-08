#pragma once

#include "engine/engine.h"

#include <memory>

class lua_State;

namespace thrive {

class ScriptEngine : public Engine {

public:

    ScriptEngine(
        lua_State* luaState
    );

    ~ScriptEngine();

    void 
    init(
        EntityManager* entityManager
    ) override;

    lua_State*
    luaState();

    void 
    shutdown() override;

    void
    update() override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

