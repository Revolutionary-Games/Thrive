#include "ogre/script_bindings.h"

#include "scripting/lua_state.h"
#include "scripting/script_initializer.h"

#include <gtest/gtest.h>
#include <luabind/luabind.hpp>
#include <OgreVector3.h>

using namespace Ogre;
using namespace luabind;
using namespace thrive;

TEST(OgreVector3, Lua) {
    LuaState L;
    initializeLua(L);
    object globals = luabind::globals(L);
    L.doString(
        "a = Vector3(1, 2, 3)\n"
        "b = Vector3(10, 20, 30)\n"
        "sum = a + b\n"
        "dot = a:dotProduct(b)\n"
    );
    Vector3 sum = object_cast<Vector3>(globals["sum"]);
    Real dot = object_cast<Real>(globals["dot"]);
    EXPECT_EQ(Vector3(11, 22, 33), sum);
    EXPECT_EQ(140, dot);
}
