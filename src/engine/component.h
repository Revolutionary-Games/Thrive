#pragma once

#include "engine/scripting.h"

#include <string>
#include <unordered_map>

namespace thrive {

class PropertyBase;
class SignalBase;

class Component {

public:

    using TypeId = size_t;

    using PropertyMap = std::unordered_map<
        std::string, 
        PropertyBase&
    >;

    using SignalMap = std::unordered_map<
        std::string, 
        SignalBase&
    >;

    static Component*
    getFromLua(
        lua_State* L,
        int index
    );

    virtual ~Component() = 0;

    const PropertyMap&
    properties() const;

    virtual int
    pushToLua(
        lua_State* L
    );

    void 
    registerProperty(
        PropertyBase& property
    );

    void 
    registerSignal(
        std::string name,
        SignalBase& signal
    );

    const SignalMap&
    signals() const;

    virtual TypeId
    typeId() const = 0;

    virtual const std::string&
    typeString() const = 0;

private:

    PropertyMap m_properties;

    SignalMap m_signals;

};


#define COMPONENT(type_)  \
    public: \
        \
        static const TypeId TYPE_ID = __COUNTER__; \
        \
        TypeId typeId() const override { \
            return TYPE_ID; \
        } \
        \
        const std::string& typeString() const override { \
            static std::string string(#type_); \
            return string; \
        } \
        \
    private:


}
