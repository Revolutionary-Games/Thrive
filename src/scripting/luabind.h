#pragma once

#include <memory>

namespace luabind { namespace detail { namespace has_get_pointer_ {

    template<typename T>
    inline T* 
    get_pointer(
        const std::shared_ptr<T>& ptr
    ) {
        return ptr.get();
    }


    template<typename T>
    inline T* 
    get_pointer(
        std::shared_ptr<T>& ptr
    ) {
        return ptr.get();
    }

}}}

namespace boost {

    template<typename T>
    inline T* 
    get_pointer(
        std::shared_ptr<T>& ptr
    ) {
        return ptr.get();
    }

}

#include <luabind/luabind.hpp>
