#include "scripting/script_initializer.h"

#include "common/movement.h"
#include "common/transform.h"
#include "engine/component.h"
#include "engine/entity.h"
#include "ogre/on_key.h"
#include "ogre/mesh_system.h"
#include "ogre/script_bindings.h"
#include "ogre/sky_system.h"
#include "scripting/luabind.h"
#include "scripting/on_update.h"

#include <forward_list>
#include <iostream>

using namespace thrive;

static void
debug(
    const std::string& msg
) {
    std::cout << msg << std::endl;
}

struct ScriptInitializer::Implementation {

    luabind::scope m_bindings;

};


ScriptInitializer&
ScriptInitializer::instance() {
    static ScriptInitializer instance;
    return instance;
}


ScriptInitializer::ScriptInitializer()
  : m_impl(new Implementation())
{
}


ScriptInitializer::~ScriptInitializer() {}


bool
ScriptInitializer::addBindings(
    luabind::scope bindings
) {
    // It looks weird, but works. 
    // luabind::scope has an overloaded comma operator
    m_impl->m_bindings, bindings;
    return true;
}


void
ScriptInitializer::initialize(
    lua_State* L
) {
    luabind::open(L);
    luabind::module(L) [
        luabind::def("debug", debug),
        // Base classes
        Component::luaBindings(),
        Entity::luaBindings(),
        // Common components
        MovableComponent::luaBindings(),
        TransformComponent::luaBindings(),
        // Script Components
        OnUpdateComponent::luaBindings(),
        // Rendering Components
        OgreBindings::luaBindings(),
        OnKeyComponent::luaBindings(),
        MeshComponent::luaBindings(),
        SkyPlaneComponent::luaBindings()
    ];
}



