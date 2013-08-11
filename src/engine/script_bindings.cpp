#include "engine/script_bindings.h"

#include "engine/component.h"
#include "engine/entity.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"

luabind::scope
thrive::EngineBindings::luaBindings() {
    return (
        System::luaBindings(),
        Component::luaBindings(),
        Entity::luaBindings(),
        Touchable::luaBindings()
    );
}
