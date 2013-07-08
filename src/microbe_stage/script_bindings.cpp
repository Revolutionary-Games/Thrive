#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/movement.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        MicrobeMovementComponent::luaBindings()
    );
}


