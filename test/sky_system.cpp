#include "ogre/sky_system.h"

#include "scripting/script_initializer.h"

#include "scripting/luajit.h"

#include "util/make_unique.h"

#include <gtest/gtest.h>

using namespace thrive;


TEST(SkyPlaneComponent, ScriptBindings)
{
    sol::state lua;

    initializeLua(lua);

    auto skyPlane = make_unique<SkyPlaneComponent>();
    lua["skyPlane"] = skyPlane.get();

    ;

    // Enabled
    EXPECT_TRUE(lua.do_string("skyPlane.properties.enabled = false").valid());

    EXPECT_FALSE(skyPlane->m_properties.enabled);
    // Plane.d
    EXPECT_TRUE(lua.do_string("skyPlane.properties.plane.d = 42.0").valid());
    EXPECT_EQ(42.0f, skyPlane->m_properties.plane.d);
}
