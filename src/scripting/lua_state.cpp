#include "scripting/lua_state.h"

#include <assert.h>

#include "lauxlib.h"
#include "lualib.h"

using namespace thrive;

LuaState::LuaState()
  : m_state(luaL_newstate())
{
    luaL_openlibs(m_state);
}


LuaState::~LuaState() {
    lua_close(m_state);
}


LuaState::LuaState(
    LuaState&& other
) : m_state(other.m_state)
{
    other.m_state = nullptr;
}


LuaState&
LuaState::operator= (
    LuaState&& other
) {
    assert(this != &other);
    m_state = other.m_state;
    other.m_state = nullptr;
    return *this;
}


LuaState::operator lua_State* () {
    return m_state;
}


bool
LuaState::doFile(
    const std::string& filename
) {
    return not luaL_dofile(m_state, filename.c_str());
}


bool
LuaState::doString(
    const std::string& string
) {
    return not luaL_dostring(m_state, string.c_str());
}
