#include "scripting/script_bindings.h"

#include "scripting/luabind.h"
#include "scripting/on_update.h"

luabind::scope
thrive::ScriptBindings::luaBindings() {
    return (
        OnUpdateComponent::luaBindings()
    );
}

