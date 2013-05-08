#pragma once

#include "engine/typedefs.h"

#include <memory>
#include <string>
#include <unordered_map>

namespace luabind {
class scope;
}

namespace thrive {

class Component {

public:

    using TypeId = size_t;

    static TypeId
    generateTypeId();

    static luabind::scope
    luaBindings();

    virtual ~Component() = 0;

    virtual TypeId
    typeId() const = 0;

    virtual const std::string&
    typeName() const = 0;


};

}

#define COMPONENT(name)  \
    public: \
        \
        static TypeId TYPE_ID() { \
            static TypeId id = Component::generateTypeId(); \
            return id; \
        } \
        \
        TypeId typeId() const override { \
            return TYPE_ID(); \
        } \
        \
        static const std::string& TYPE_NAME() { \
            static std::string string(#name); \
            return string; \
        } \
        \
        const std::string& typeName() const override { \
            return TYPE_NAME(); \
        } \
        \
    private: \


