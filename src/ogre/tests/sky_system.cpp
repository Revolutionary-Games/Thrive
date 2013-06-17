#include "ogre/sky_system.h"

#include "ogre/script_bindings.h"
#include "scripting/lua_state.h"
#include "scripting/tests/do_string_assertion.h"

#include <gtest/gtest.h>
#include <luabind/luabind.hpp>

using namespace thrive;


TEST(SkyPlaneComponent, ScriptBindings) {
    LuaState L;
    luabind::open(L);
    luabind::module(L)[
        OgreBindings::luaBindings(),
        Component::luaBindings(),
        SkyPlaneComponent::luaBindings()
    ];
    luabind::object globals = luabind::globals(L);
    auto skyPlane = std::make_shared<SkyPlaneComponent>();
    globals["skyPlane"] = skyPlane.get();
    // Enabled
    EXPECT_TRUE(LuaSuccess(L,
        "skyPlane.enabled = false"
    ));
    EXPECT_FALSE(skyPlane->enabled);
    // Plane.d
    EXPECT_TRUE(LuaSuccess(L,
        "skyPlane.plane.d = 42.0"
    ));
    EXPECT_EQ(42.0f, skyPlane->plane.d);
}
