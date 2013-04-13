#include "engine/component.h"

#include "engine/property.h"

#include <assert.h>
#include <tuple>
#include <iostream>

using namespace thrive;

Component*
Component::getFromLua(
    lua_State*,
    int
) {
    return nullptr;
}


Component::~Component() {}


const Component::PropertyMap&
Component::properties() const {
    return m_properties;
}


const Component::SignalMap&
Component::signals() const {
    return m_signals;
}


void
Component::registerProperty(
    PropertyBase& property
) {
    bool isUnique = false;
    std::tie(std::ignore, isUnique) = m_properties.insert(
        std::pair<std::string, PropertyBase&>(property.name(), property)
    );
    assert(isUnique && "Duplicate property name");
}


void
Component::registerSignal(
    std::string name,
    SignalBase& signal
) {
    bool isUnique = false;
    std::tie(std::ignore, isUnique) = m_signals.insert(
        std::pair<std::string, SignalBase&>(name, signal)
    );
    assert(isUnique && "Duplicate signal name");
}


static const char* UNKNOWN_PROPERTY_MESSAGE = "Unknown property: \"%s\"";


static int component_index(
    lua_State* L
) {
    Component* component = static_cast<Component*>(lua_touserdata(L, 1));
    if (not component) {
        return luaL_error(L, "Not a component");
    }
    const char* key = lua_tostring(L, 2);
    // Check properties first
    const Component::PropertyMap& properties = component->properties();
    auto propertyIter = properties.find(key);
    if (propertyIter != properties.cend()) {
        return propertyIter->second.pushToLua(L);
    }
    // Then signals
    const Component::SignalMap& signals = component->signals();
    auto signalIter = signals.find(key);
    if (signalIter != signals.cend()) {
        return signalIter->second.pushToLua(L);
    }
    // Nope, nothing here
    return luaL_error(L, UNKNOWN_PROPERTY_MESSAGE, key);
}

static int component_newindex(
    lua_State* L
) {
    Component* component = static_cast<Component*>(lua_touserdata(L, 1));
    if (not component) {
        return luaL_error(L, "Invalid Component table");
    }
    const Component::PropertyMap& properties = component->properties();
    const char* key = lua_tostring(L, 2);
    auto iter = properties.find(key);
    if (iter == properties.cend()) {
        return luaL_error(L, UNKNOWN_PROPERTY_MESSAGE, key);
    }
    return iter->second.getFromLua(L, 3);
}

int
Component::pushToLua(
    lua_State* L
) {
    const char* typeName = this->typeString().c_str();
    bool isNew = luaL_newmetatable(L, typeName);
    if (isNew) {
        lua_pushcfunction(L, component_index);
        lua_setfield(L, -2, "__index");
        lua_pushcfunction(L, component_newindex);
        lua_setfield(L, -2, "__newindex");
    }
    lua_pop(L, 1);
    lua_pushlightuserdata(L, this);
    luaL_setmetatable(L, typeName);
    return 1;
}


