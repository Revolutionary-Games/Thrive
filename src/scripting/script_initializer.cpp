#include "scripting/script_initializer.h"

#include "bullet/script_bindings.h"
#include "engine/engine.h"
#include "engine/script_bindings.h"
#include "game.h"
#include "microbe_stage/script_bindings.h"
#include "ogre/keyboard_system.h"
#include "ogre/script_bindings.h"
#include "scripting/luabind.h"
#include "scripting/script_bindings.h"

#include <forward_list>
#include <iostream>

static void
debug(
    const std::string& msg
) {
    std::cout << msg << std::endl;
}

void
thrive::initializeLua(
    lua_State* L
) {
    luabind::open(L);
    luabind::module(L) [
        luabind::def("debug", debug),
        EngineBindings::luaBindings(),
        OgreBindings::luaBindings(),
        BulletBindings::luaBindings(),
        ScriptBindings::luaBindings(),
        MicrobeBindings::luaBindings()
    ];
    luabind::object globals = luabind::globals(L);
    globals["Keyboard"] = &(Game::instance().engine().keyboardSystem());
}



