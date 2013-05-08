
#include "engine/scripting.h"
#include "LuaBridge.h"

#include <gtest/gtest.h>
#include <memory>

using namespace thrive;

class Base {

public:

    virtual void print() {
        std::cout << "Base::print()" << std::endl;
    }
};


class Derived : public Base {

public:

    void print() override {
        std::cout << "Derived::print()" << std::endl;
    }
};


TEST(LuaBridge, Derived) {
    LuaState L;
    luabridge::getGlobalNamespace(L)
        .beginNamespace("test")
            .beginClass<Base>("Base")
                .addConstructor<void(*)(void)>()
                .addFunction("print", &Base::print)
            .endClass()
            .deriveClass<Derived, Base>("Derived")
                .addConstructor<void(*)(void)>()
            .endClass()
        .endNamespace();
    EXPECT_TRUE(not luaL_dostring(
        L, 
        "derived = test.Derived()\n"
        "derived:print()"
    ));
    std::unique_ptr<Base> alias(new Derived());
    luabridge::Stack<Derived>::push(L, *alias);
    lua_setglobal(L, "base");
    EXPECT_TRUE(not luaL_dostring(
        L, 
        "base:print()"
    ));
}

