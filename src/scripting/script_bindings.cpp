#include "scripting/script_bindings.h"

#include "scripting/luabind.h"
#include "scripting/script_entity_filter.h"

luabind::scope
thrive::ScriptBindings::luaBindings() {
    return (
        ScriptEntityFilter::luaBindings()
    );
}

