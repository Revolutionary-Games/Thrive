#include "scripting/script_bindings.h"

#include "scripting/luabind.h"
#include "scripting/on_update.h"
#include "scripting/script_component.h"
#include "scripting/script_entity_filter.h"

luabind::scope
thrive::ScriptBindings::luaBindings() {
    return (
        OnUpdateComponent::luaBindings(),
        ScriptComponent::luaBindings(),
        ScriptEntityFilter::luaBindings()
    );
}

