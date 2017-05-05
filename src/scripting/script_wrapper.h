#pragma once

#include "luajit.h"

namespace thrive {

//! \brief Base class for classes that support "inheriting" from in Lua
class ScriptWrapper{
public:
    ScriptWrapper(sol::table obj) : m_luaObject(obj){
        
    }

protected:

    //! This is the lua table that contains the overridden functions
    //!
    //! This might also contain the regular functions bound from C++.
    //! Which isn't optimal because they will have an overhead when
    //! calling them through Lua
    sol::table m_luaObject;
};


}
