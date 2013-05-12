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



