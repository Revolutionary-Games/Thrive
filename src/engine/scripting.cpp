#include "engine/scripting.h"

#include "engine/component.h"

using namespace thrive;

LuaState::LuaState()
  : m_state(luaL_newstate())
{
}


LuaState::~LuaState() {
    lua_close(m_state);
}


LuaState::operator lua_State*() const {
    return m_state;
}


////////////////////////////////////////////////////////////////////////////////
// Stack Manipulators
////////////////////////////////////////////////////////////////////////////////

////////////
// Double //
////////////

int
LuaStack<double>::push(
    lua_State* L,
    double value
) {
    lua_pushnumber(L, value);
    return 1;
}

double
LuaStack<double>::get(
    lua_State* L,
    int index
) {
    return luaL_checknumber(L, index);
}


/////////
// int //
/////////

int
LuaStack<int>::push(
    lua_State* L,
    int value
) {
    lua_pushnumber(L, value);
    return 1;
}

int
LuaStack<int>::get(
    lua_State* L,
    int index
) {
    return luaL_checknumber(L, index);
}


/////////////////
// const char* //
/////////////////

int
LuaStack<const char*>::push(
    lua_State* L,
    const char* value
) {
    lua_pushstring(L, value);
    return 1;
}

const char*
LuaStack<const char*>::get(
    lua_State* L,
    int index
) {
    return luaL_checkstring(L, index);
}


//////////
// bool //
//////////

int
LuaStack<bool>::push(
    lua_State* L,
    bool value
) {
    lua_pushboolean(L, value);
    return 1;
}

bool
LuaStack<bool>::get(
    lua_State* L,
    int index
) {
    return lua_toboolean(L, index);
}


///////////////
// Component //
///////////////

int
LuaStack<Component>::push(
    lua_State* L,
    Component& component
) {
    return component.pushToLua(L);
}


Component*
LuaStack<Component>::get(
    lua_State* state,
    int index
) {
    return Component::getFromLua(state, index);
}



