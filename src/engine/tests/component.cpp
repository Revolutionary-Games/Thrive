#include "engine/component.h"

#include "engine/property.h"
#include "engine/scripting.h"

#include "engine/tests/test_component.h"

#include <gtest/gtest.h>
#include <iostream>

using namespace thrive;

TEST (Property, Set) {
    TestComponent component;
    bool emittedValue = false;
    EXPECT_FALSE(component.p_bool);
    component.p_bool.sig_valueChanged.connect(
        [&emittedValue](bool value) {
            emittedValue = value;
        }
    );
    component.p_bool = true;
    EXPECT_TRUE(component.p_bool);
    EXPECT_TRUE(emittedValue);
}


TEST (Property, Lua) {
    LuaState L;
    TestComponent component;
    lua_pushboolean(L, true);
    component.p_bool.getFromLua(L, -1);
    EXPECT_TRUE(component.p_bool);
}


TEST (Component, LuaProperties) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // C++ => Lua
    component.p_double = 5.0;
    luaL_dostring(L, "value = component.double");
    lua_getglobal(L, "value");
    EXPECT_EQ(5.0, lua_tonumber(L, -1));
    lua_pop(L, 1);
    // Lua => C++
    luaL_dostring(L, "component.boolean = true");
    EXPECT_TRUE(component.p_bool);
}


TEST(Component, LuaSignals) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // C++ => Lua
    if(luaL_dostring(L, "component.sig_doubleChanged.connect(function(v) value = v end)")) {
        FAIL() << "Lua Error: " << lua_tostring(L, -1);
    }
    component.p_double = 2.0;
    lua_getglobal(L, "value");
    EXPECT_EQ(2.0, lua_tonumber(L, -1));
}


TEST (Component, ReadUnknownProperty) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // C++ => Lua
    EXPECT_TRUE(luaL_dostring(L, "value = component.nothing"));
}


TEST (Component, WriteUnknownProperty) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // C++ => Lua
    EXPECT_TRUE(luaL_dostring(L, "component.nothing = true"));
}


TEST (Component, TypeMismatch) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // C++ => Lua
    EXPECT_TRUE(luaL_dostring(L, "component.double = true"));
}


TEST (Component, DisconnectSignal) {
    LuaState L;
    TestComponent component;
    LuaStack<Component>::push(L, component);
    lua_setglobal(L, "component");
    // Connect signal
    if(luaL_dostring(L, "connection = component.sig_doubleChanged.connect(function(v) value = v end)")) {
        FAIL() << "Lua Error: " << lua_tostring(L, -1);
    }
    component.p_double = 2.0;
    lua_getglobal(L, "value");
    EXPECT_EQ(2.0, lua_tonumber(L, -1));
    // Disconnect
    EXPECT_TRUE(luaL_dostring(L, "temp = connection.nothing"));
    EXPECT_TRUE(luaL_dostring(L, "connection.nothing = 5"));
    if(luaL_dostring(L, "connection.disconnect()")) {
        FAIL() << "Lua Error: " << lua_tostring(L, -1);
    }
    component.p_double = 4.0;
    lua_getglobal(L, "value");
    EXPECT_EQ(2.0, lua_tonumber(L, -1));
}

