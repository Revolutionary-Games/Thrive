#pragma once

#include <boost/version.hpp>
#include <memory>

namespace luabind { namespace detail { namespace has_get_pointer_ {

    template<typename T>
    inline T* 
    get_pointer(
        const std::unique_ptr<T>& ptr
    ) {
        return ptr.get();
    }


    template<typename T>
    inline T* 
    get_pointer(
        std::unique_ptr<T>& ptr
    ) {
        return ptr.get();
    }


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

#if (BOOST_VERSION / 100 % 1000) < 53

namespace boost {

    template<typename T>
    inline T* 
    get_pointer(
        const std::unique_ptr<T>& ptr
    ) {
        return ptr.get();
    }


    template<typename T>
    inline T* 
    get_pointer(
        const std::shared_ptr<T>& ptr
    ) {
        return ptr.get();
    }

}

#endif // Boost version

#include <luabind/luabind.hpp>
