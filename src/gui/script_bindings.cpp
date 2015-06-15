#include "gui/script_bindings.h"

#include "gui/CEGUIWindow.h"
#include "script_wrappers.h"
#include "scripting/luabind.h"

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

static void
ItemEntry_setText(
    CEGUI::ItemEntry* self,
    const std::string& text
) {
    self->setText(text);
}

static bool
ItemEntry_isSelected(
    CEGUI::ItemEntry* self
) {
    return self->isSelected();
}

static void
ItemEntry_select(
    CEGUI::ItemEntry* self
) {
    self->select();
}

static void
ItemEntry_deselect(
    CEGUI::ItemEntry* self
) {
    self->deselect();
}

static void
ItemEntry_setSelectable(
    CEGUI::ItemEntry* self,
    bool setting
) {
    self->setSelectable(setting);
}

static luabind::scope
itemEntryBindings() {
    return class_<CEGUI::ItemEntry>("ItemEntry")
        .def(constructor<const std::string&, const std::string&>())
        .def("isSelected", &ItemEntry_isSelected)
        .def("select", &ItemEntry_select)
        .def("deselect", &ItemEntry_deselect)
        .def("setSelectable", &ItemEntry_setSelectable)
        .def("setText", &ItemEntry_setText)
    ;
}

luabind::scope
thrive::GuiBindings::luaBindings() {
    return (
        // Other
        listboxItemBindings(),
        itemEntryBindings(),
        CEGUIWindow::luaBindings(),
        ScriptWrappers::StandardItemWrapperBindings()
    );
}


