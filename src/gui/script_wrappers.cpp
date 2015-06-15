#include "script_wrappers.h"

#include <CEGUI/CEGUI.h>
#include <CEGUI/views/StandardItemModel.h>
#include "scripting/luabind.h"

using namespace thrive;
using namespace luabind;

luabind::scope
ScriptWrappers::StandardItemWrapperBindings() {
    return class_<StandardItemWrapper>("StandardItemWrapper")
        .def(constructor<const std::string&, int>())
        ;
}


StandardItemWrapper::StandardItemWrapper(
    const std::string &text,
    int id) :
    m_attached(false)
{
        
    m_item = new CEGUI::StandardItem(text, id);
}

StandardItemWrapper::~StandardItemWrapper(){

    if(!m_attached && m_item){

        delete m_item;
        m_item = nullptr;
    }
}

CEGUI::StandardItem*
StandardItemWrapper::getItem(){

    return m_item;
}

void
StandardItemWrapper::markAttached(){

    m_attached = true;
}


