#include "engine/touchable.h"

#include "scripting/luabind.h"

using namespace thrive;


luabind::scope
Touchable::luaBindings() {
    using namespace luabind;
    return class_<Touchable>("Touchable")
        .def("hasChanges", &Touchable::hasChanges)
        .def("touch", &Touchable::touch)
        .def("untouch", &Touchable::untouch)
    ;
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
