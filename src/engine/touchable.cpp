#include "engine/touchable.h"

#include "scripting/luajit.h"

using namespace thrive;

void Touchable::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Touchable>("Touchable",

        "hasChanges", &Touchable::hasChanges,
        "touch", &Touchable::touch,
        "untouch", &Touchable::untouch
    );
}

bool
Touchable::hasChanges() const {
    return m_hasChanges;
}


void
Touchable::touch() {
    m_hasChanges = true;
}


void
Touchable::untouch() {
    m_hasChanges = false;
}
