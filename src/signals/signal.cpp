#include "signals/signal.h"

#include "engine/component.h"

#include <cstring>

using namespace thrive;

SignalBase::~SignalBase() {
    for(auto pair : m_luaReferences) {
        luaL_unref(pair.first, LUA_REGISTRYINDEX, pair.second);
    }
    for(auto pair : m_luaSlots) {
        luaL_unref(pair.first, LUA_REGISTRYINDEX, pair.second);
    }
}


void
SignalBase::disconnectFromLua(
    lua_State* L,
    int slotReference
) {
    m_removedLuaSlots.push_back(std::make_pair(L, slotReference));
}


static int
connection_disconnect(
    lua_State* L
) {
    auto signal = static_cast<SignalBase*>(
        lua_touserdata(L, lua_upvalueindex(1))
    );
    int signalReference = lua_tointeger(L, lua_upvalueindex(2));
    int slotReference = lua_tointeger(L, lua_upvalueindex(3));
    lua_rawgeti(L, LUA_REGISTRYINDEX, signalReference);
    if (signal == lua_touserdata(L, -1)) {
        // The signal is still alive
        signal->disconnectFromLua(L, slotReference);
    }
    return 0;
}


static int
connection_index(
    lua_State* L
) {
    const char* key = luaL_checkstring(L, 2);
    if (std::strcmp(key, "disconnect") == 0) {
        lua_getfield(L, 1, "signalUserData");
        lua_getfield(L, 1, "signalReference");
        lua_getfield(L, 1, "slotReference");
        lua_pushcclosure(L, connection_disconnect, 3);
        return 1;
    }
    else {
        return luaL_error(L, "Unknown Key: %s", key);
    }
}


int
SignalBase::connectToLua(
    lua_State* L
) {
    luaL_checktype(L, 1, LUA_TFUNCTION);
    if (lua_gettop(L) > 1) {
        return luaL_error(L, "Too many function arguments");
    }
    int reference = luaL_ref(L, LUA_REGISTRYINDEX);
    m_luaSlots.push_front(std::make_pair(L, reference));
    // Put a reference to this signal, if none
    if (m_luaReferences.find(L) == m_luaReferences.end()) {
        lua_pushlightuserdata(L, this);
        m_luaReferences[L] = luaL_ref(L, LUA_REGISTRYINDEX);
    }
    // Build connection object, push to lua stack
    bool isNew = luaL_newmetatable(L, "SignalConnection");
    if (isNew) {
        lua_pushcfunction(L, connection_index);
        lua_setfield(L, -2, "__index");
        lua_pushboolean(L, false);
        lua_setfield(L, -2, "__newindex");
    }
    lua_pop(L, 1);
    lua_newtable(L);
    lua_pushlightuserdata(L, this);
    lua_setfield(L, -2, "signalUserData");
    lua_pushinteger(L, m_luaReferences[L]);
    lua_setfield(L, -2, "signalReference");
    lua_pushinteger(L, reference);
    lua_setfield(L, -2, "slotReference");
    luaL_setmetatable(L, "SignalConnection");
    return 1;
}


static int
signal_connectToLua(
    lua_State* L
) {
    auto signal = static_cast<SignalBase*>(
        lua_touserdata(L, lua_upvalueindex(1))
    );
    return signal->connectToLua(L);
    
}


static int 
signal_index(
    lua_State* L
) {
    auto signal = static_cast<SignalBase*>(luaL_checkudata(L, 1, "Signal"));
    const char* key = luaL_checkstring(L, 2);
    if (std::strcmp(key, "connect") == 0) {
        lua_pushlightuserdata(L, signal);
        lua_pushcclosure(L, signal_connectToLua, 1);
        return 1;
    }
    else {
        return luaL_error(L, "Key not found: %s", key);
    }
}


int
SignalBase::pushToLua(
    lua_State* L
) {
    bool isNew = luaL_newmetatable(L, "Signal");
    if (isNew) {
        lua_pushcfunction(L, signal_index);
        lua_setfield(L, -2, "__index");
        lua_pushboolean(L, false);
        lua_setfield(L, -2, "__newindex");
    }
    lua_pop(L, 1);
    lua_pushlightuserdata(L, this);
    luaL_setmetatable(L, "Signal");
    return 1;
}


void
SignalBase::removeStaleLuaSlots() {
    for (auto pair: m_removedLuaSlots) {
        lua_State* L = pair.first;
        int slotReference = pair.second;
        luaL_unref(L, LUA_REGISTRYINDEX, slotReference);
        m_luaSlots.remove(pair);
    }
}
