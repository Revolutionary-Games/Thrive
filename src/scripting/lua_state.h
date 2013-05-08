#pragma once

#include <string>

class lua_State;

namespace thrive {

class LuaState {

public:

    LuaState();

    LuaState(const LuaState&) = delete;

    LuaState(LuaState&& other);

    ~LuaState();

    LuaState&
    operator= (const LuaState&) = delete;

    LuaState&
    operator= (LuaState&& other);

    operator lua_State* ();

    bool
    doFile(
        const std::string& filename
    );

    bool
    doString(
        const std::string& string
    );

private:

    lua_State* m_state;
};

}
