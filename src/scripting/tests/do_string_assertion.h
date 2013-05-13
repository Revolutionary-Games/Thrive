#pragma once

#include <gtest/gtest.h>
#include <luabind/luabind.hpp>
#include <lualib.h>
#include <lauxlib.h>

static ::testing::AssertionResult 
LuaSuccess(
    lua_State* L,
    const std::string& string
) {
    if (luaL_dostring(L, string.c_str())) {
        luabind::object error_msg(
            luabind::from_stack(L, -1)
        );
        return ::testing::AssertionFailure() << error_msg;
    }
    else {
        return ::testing::AssertionSuccess();
    }
}

