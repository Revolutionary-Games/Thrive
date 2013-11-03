#include "bullet/script_bindings.h"

#include "bullet/bullet_ogre_conversion.h"
#include "bullet/bullet_to_ogre_system.h"
#include "bullet/collision_filter.h"
#include "bullet/collision_shape.h"
#include "bullet/collision_system.h"
#include "bullet/debug_drawing.h"
#include "bullet/rigid_body_system.h"
#include "bullet/update_physics_system.h"
#include "scripting/luabind.h"

#include <utility>
#include <btBulletCollisionCommon.h>
#include <memory>
#include <OgreVector3.h>

using namespace luabind;
using namespace thrive;


luabind::scope
thrive::BulletBindings::luaBindings() {
    return (
        // Shapes
        CollisionShape::luaBindings(),
        BoxShape::luaBindings(),
        CapsuleShape::luaBindings(),
        CompoundShape::luaBindings(),
        ConeShape::luaBindings(),
        CylinderShape::luaBindings(),
        EmptyShape::luaBindings(),
        SphereShape::luaBindings(),
        // Components
        RigidBodyComponent::luaBindings(),
        CollisionComponent::luaBindings(),
        // Systems
        BulletToOgreSystem::luaBindings(),
        RigidBodyInputSystem::luaBindings(),
        RigidBodyOutputSystem::luaBindings(),
        BulletDebugDrawSystem::luaBindings(),
        UpdatePhysicsSystem::luaBindings(),
        CollisionSystem::luaBindings(),
        // Other
        CollisionFilter::luaBindings(),
        Collision::luaBindings()
    );
}
