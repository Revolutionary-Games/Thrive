#include "ogre/sky_system.h"

#include "ogre/script_bindings.h"
#include "scripting/lua_state.h"
#include "scripting/tests/do_string_assertion.h"
#include "scripting/script_initializer.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>
#include <luabind/luabind.hpp>

using namespace thrive;


TEST(SkyPlaneComponent, ScriptBindings) {
    LuaState L;
    initializeLua(L);
    luabind::object globals = luabind::globals(L);
    auto skyPlane = make_unique<SkyPlaneComponent>();
    globals["skyPlane"] = skyPlane.get();
    // Enabled
    EXPECT_TRUE(LuaSuccess(L,
        "skyPlane.properties.enabled = false"
    ));
    EXPECT_FALSE(skyPlane->m_properties.enabled);
    // Plane.d
    EXPECT_TRUE(LuaSuccess(L,
        "skyPlane.properties.plane.d = 42.0"
    ));
    EXPECT_EQ(42.0f, skyPlane->m_properties.plane.d);
}
