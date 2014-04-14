#include "gui/script_bindings.h"

#include "scripting/luabind.h"
#include "gui/CEGUIWindow.h"

using namespace luabind;

static void
ListboxItem_setColour(
    CEGUI::ListboxTextItem* self,
    float r,
    float g,
    float b
) {
    self->setTextColours(CEGUI::Colour(r,g,b));
}

static void
ListboxItem_setText(
    CEGUI::ListboxTextItem* self,
    const std::string& text
) {
    self->setText(text);
}


static luabind::scope
listboxItemBindings() {
    return class_<CEGUI::ListboxTextItem>("ListboxItem")
        .def(constructor<const std::string&>())
        .def("setTextColours", &ListboxItem_setColour)
        .def("setText", &ListboxItem_setText)
    ;
}

luabind::scope
thrive::GuiBindings::luaBindings() {
    return (
        // Other
        listboxItemBindings(),
        CEGUIWindow::luaBindings()
    );
}


