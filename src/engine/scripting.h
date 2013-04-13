#pragma once

#include "lua.h"
#include "lauxlib.h"

#include <utility>

namespace thrive {

class LuaState {

public:

    LuaState();

    ~LuaState();

    LuaState(const LuaState&) = delete;

    LuaState&
    operator= (const LuaState&) = delete;

    operator lua_State*() const;

private:

    lua_State* m_state;
};


template<typename... Args>
struct LuaStack {
    
    static int
    push(
        lua_State* L,
        Args... args
    );
};

template<typename Head, typename... Tail>
struct LuaStack<Head, Tail...> {
    
    static int
    push(
        lua_State* L,
        Head&& head,
        Tail&&... tail
    ) {
        int headCount = LuaStack<Head>::push(L, std::forward<Head>(head));
        int tailCount = LuaStack<Tail...>::push(L, std::forward<Tail>(tail)...);
        return headCount + tailCount;
    }
};

template<>
struct LuaStack<> {

    static int
    push(
        lua_State*
    ) {
        return 0;
    }
};


template<typename T>
struct LuaStack<const T> {

    static int
    push(
        lua_State* L,
        const T value
    ) {
        return LuaStack<T>::push(L, value);
    }


    static auto
    get(
        lua_State* L,
        int index
    ) -> decltype(LuaStack<T>::get(L, index)) {
        return LuaStack<T>::get(L, index);
    }


};


template<typename T>
struct LuaStack<T&> {

    static int
    push(
        lua_State* L,
        T& value
    ) {
        return LuaStack<T>::push(L, value);
    }

    static auto get(
        lua_State* L,
        int index
    ) -> decltype(LuaStack<T>::get(L, index)) {
        return LuaStack<T>::get(L, index);
    }

};


template<>
struct LuaStack<bool> {

    static int
    push(
        lua_State* state,
        bool value
    );

    static bool
    get(
        lua_State* state,
        int index
    );
};


template<>
struct LuaStack<double> {

    static int
    push(
        lua_State* state,
        double value
    );

    static double
    get(
        lua_State* state,
        int index
    );
};


template<>
struct LuaStack<int> {

    static int
    push(
        lua_State* state,
        int value
    );

    static int
    get(
        lua_State* state,
        int index
    );
};


template<class T>
struct LuaStack<T> {

    static typename std::enable_if<std::is_integral<T>::value, int>::type
    push(
        lua_State* L,
        T value
    ) {
        return LuaStack<int>::push(L, value);
    }

    static typename std::enable_if<std::is_integral<T>::value>::type
    get(
        lua_State* L,
        int index
    ) {
        return LuaStack<int>::get(L, index);
    }
};


template<>
struct LuaStack<const char*> {

    static int
    push(
        lua_State* state,
        const char* value
    );

    static const char*
    get(
        lua_State* state,
        int index
    );
};


class Component;

template<>
struct LuaStack<Component> {

    static int
    push(
        lua_State* state,
        Component& value
    );

    static Component*
    get(
        lua_State* state,
        int index
    );
};

}
