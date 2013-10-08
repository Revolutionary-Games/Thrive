#include "bullet/script_bindings.h"

#include "bullet/bullet_ogre_conversion.h"
#include "bullet/collision_shape.h"
#include "bullet/rigid_body_system.h"
#include "scripting/luabind.h"

#include <btBulletCollisionCommon.h>
#include <memory>
#include <OgreVector3.h>

using namespace luabind;
using namespace thrive;

luabind::scope
thrive::BulletBindings::luaBindings() {
    return (
        CollisionShape::luaBindings(),
        BoxShape::luaBindings(),
        CapsuleShape::luaBindings(),
        CompoundShape::luaBindings(),
        ConeShape::luaBindings(),
        CylinderShape::luaBindings(),
        EmptyShape::luaBindings(),
        SphereShape::luaBindings(),
        RigidBodyComponent::luaBindings()
    );
}

