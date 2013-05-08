#include "oolua.h"

#include <gtest/gtest.h>
#include <iostream>
#include <memory>


class Base {

public:

    virtual void print() {
        std::cout << "Base::print()" << std::endl;
    }
};


OOLUA_PROXY_CLASS(Base)
    OOLUA_NO_TYPEDEFS
    OOLUA_ONLY_DEFAULT_CONSTRUCTOR
    OOLUA_MEM_FUNC(void, print)
OOLUA_CLASS_END

EXPORT_OOLUA_FUNCTIONS_NON_CONST(Base,
    print
)

EXPORT_OOLUA_FUNCTIONS_CONST(Base)


static void
print(
    const std::string& line
) {
    std::cout << line << std::endl;
}

static int print(lua_State* l) {
    OOLUA_C_FUNCTION(void, print, const std::string&);
}


class Derived : public Base {

public:

    void print() override {
        std::cout << "Derived::print()" << std::endl;
    }
};

OOLUA_PROXY_CLASS(Derived, Base)
    OOLUA_NO_TYPEDEFS
    OOLUA_ONLY_DEFAULT_CONSTRUCTOR
OOLUA_CLASS_END

EXPORT_OOLUA_FUNCTIONS_NON_CONST(Derived)

EXPORT_OOLUA_FUNCTIONS_CONST(Derived)

TEST (OOLua, Derive) {
    OOLUA::Script script;
    script.register_class<Base>();
    script.register_class<Derived>();
    std::unique_ptr<Base> derived(new Derived());
    Base* raw = derived.get();
    OOLUA::set_global(script, "derived", raw);
    std::cout << "Script:" << script.run_chunk(
        "derived:print()"
    ) << std::endl;
    std::cout << "Error:" << OOLUA::get_last_error(script) << std::endl;
}


TEST (OOLua, Properties) {
    OOLUA::Script script;
    OOLUA::set_global<const lua_CFunction>(script, "print", static_cast<lua_CFunction>(print));
    script.register_class<Base>();
    script.run_chunk(
        "base = Base:new()\n"
        "base.test = 5\n"
        "print(\"\" .. base.test)"
    );
    std::cout << "Error in properties:" << OOLUA::get_last_error(script) << std::endl;
}


TEST (OOLua, OutParameters) {

}
