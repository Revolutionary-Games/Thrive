#include "script_wrappers.h"

#include <CEGUI/CEGUI.h>
#include <CEGUI/views/StandardItemModel.h>
#include "scripting/luajit.h"

using namespace thrive;


void StandardItemWrapper::luaBindings(
    sol::state &lua
){
    lua.new_usertype<StandardItemWrapper>("StandardItemWrapper",

        sol::constructors<sol::types<const std::string&, int>>()
    );
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


