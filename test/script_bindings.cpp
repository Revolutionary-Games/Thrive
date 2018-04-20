#include "scripting/script_initializer.h"

#include "scripting/luajit.h"

#include <OgreVector3.h>
#include <gtest/gtest.h>

using namespace Ogre;
using namespace thrive;

TEST(OgreVector3, Lua)
{

    sol::state lua;

    initializeLua(lua);

    lua.do_string("a = Vector3(1, 2, 3)\n"
                  "b = Vector3(10, 20, 30)\n"
                  "sum = a + b\n"
                  "dot = a:dotProduct(b)\n");

    Vector3 sum = lua.get<Vector3>("sum");
    Real dot = lua.get<Real>("dot");
    EXPECT_EQ(Vector3(11, 22, 33), sum);
    EXPECT_EQ(140, dot);
}
