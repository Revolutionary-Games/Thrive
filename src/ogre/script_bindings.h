#pragma once

namespace luabind {
class scope;
}

namespace thrive {

struct OgreBindings {
    
    static luabind::scope
    luaBindings();

};

}
