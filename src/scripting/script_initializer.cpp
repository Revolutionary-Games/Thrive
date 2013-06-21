#include "scripting/script_initializer.h"

#include "engine/component.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "game.h"
#include "ogre/camera_system.h"
#include "ogre/entity_system.h"
#include "ogre/keyboard_system.h"
#include "ogre/light_system.h"
#include "ogre/on_key.h"
#include "ogre/scene_node_system.h"
#include "ogre/script_bindings.h"
#include "ogre/sky_system.h"
#include "ogre/viewport_system.h"
#include "scripting/luabind.h"
#include "scripting/on_update.h"
#include "bullet/bullet_lua_bindings.h"
#include "bullet/rigid_body_system.h"

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
        // Script Components
        OnUpdateComponent::luaBindings(),
        // Ogre Components
        OgreBindings::luaBindings(),
        KeyboardSystem::luaBindings(),
        OnKeyComponent::luaBindings(),
        OgreCameraComponent::luaBindings(),
        OgreEntityComponent::luaBindings(),
        OgreLightComponent::luaBindings(),
        OgreSceneNodeComponent::luaBindings(),
        SkyPlaneComponent::luaBindings(),
        OgreViewport::luaBindings(),
        OgreViewportSystem::luaBindings(),
        // Physics Components
        BulletBindings::luaBindings(),
        RigidBodyComponent::luaBindings()
    ];
    luabind::object globals = luabind::globals(L);
    globals["Keyboard"] = &(Game::instance().engine().keyboardSystem());
}



