#pragma once

#include "engine/scripting.h"
#include "engine/component.h"
#include "signals/signal.h"

#include <string>

namespace thrive {

class PropertyBase {

public:

    virtual ~PropertyBase() = 0;

    virtual int 
    getFromLua(
        lua_State* L,
        int index
    ) = 0;

    virtual int 
    pushToLua(
        lua_State* L
    ) const = 0;

    std::string
    name() const;

    Component&
    owner() const;

protected:

    PropertyBase(
        Component& owner,
        std::string name
    );

private:

    std::string m_name;

    Component& m_owner;

};


template<typename Value>
class Property : public PropertyBase {

public:

    Property(
        Component& owner,
        std::string name,
        Value initialValue = Value()
    ) : PropertyBase(owner, name),
        m_value(initialValue)
    {
        owner.registerSignal(
            "sig_" + name + "Changed",
            sig_valueChanged
        );
    }

    Property<Value>&
    operator= (Value value) {
        this->set(value);
        return *this;
    }

    operator Value() const {
        return this->get();
    }

    Value
    get() const {
        return m_value;
    }

    int
    getFromLua(
        lua_State* L,
        int index
    ) override {
        this->set(LuaStack<Value>::get(L, index));
        return 0;
    }

    int
    pushToLua(
        lua_State* L
    ) const override {
        return LuaStack<Value>::push(L, this->get());
    }

    void
    set(
        Value value
    ) {
        m_value = value;
        sig_valueChanged(value);
    }

    Signal<const Value&>
    sig_valueChanged;

private:

    Value m_value;

};

}
