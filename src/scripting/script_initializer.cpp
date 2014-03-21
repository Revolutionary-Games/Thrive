#include "scripting/script_initializer.h"

#include "bullet/script_bindings.h"
#include "engine/engine.h"
#include "engine/rng.h"
#include "engine/script_bindings.h"
#include "game.h"
#include "microbe_stage/script_bindings.h"
#include "ogre/script_bindings.h"
#include "scripting/luabind.h"
#include "scripting/script_bindings.h"
#include "sound/script_bindings.h"

#include <forward_list>
#include <iostream>
#include <luabind/class_info.hpp>

static void
debug(
    const std::string& msg
) {
    std::cout << msg << std::endl;
}

static int
constructTraceback(
    lua_State* L
) {
    lua_Debug d;
    std::stringstream traceback;
    // Error message
    traceback << lua_tostring(L, -1) << ":" << std::endl;
    lua_pop(L, 1);
    // Stacktrace
    for (
        int stacklevel = 0;
        lua_getstack(L, stacklevel, &d);
        stacklevel++
    ) {
       lua_getinfo(L, "Sln", &d);
       traceback << "    " << d.short_src << ":" << d.currentline;
       if (d.name != nullptr) {
           traceback << " (" << d.namewhat << " " << d.name << ")";
       }
       traceback << std::endl;
    }
    lua_pushstring(L, traceback.str().c_str());
    return 1;
}

void
thrive::initializeLua(
    lua_State* L
) {
    luabind::set_pcall_callback(constructTraceback);
    luabind::open(L);
    luabind::bind_class_info(L);
    luabind::module(L) [
        luabind::def("debug", debug),
        EngineBindings::luaBindings(),
        OgreBindings::luaBindings(),
        BulletBindings::luaBindings(),
        ScriptBindings::luaBindings(),
        SoundBindings::luaBindings(),
        MicrobeBindings::luaBindings()
    ];
    luabind::object globals = luabind::globals(L);
    globals["Engine"] = &(Game::instance().engine());
    globals["rng"] = &(Game::instance().engine().rng());
}



