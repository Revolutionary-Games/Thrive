#include "engine/component_registry.h"

#include <assert.h>
#include <boost/lexical_cast.hpp>
#include <stdexcept>
#include <unordered_map>

#include <iostream>

using namespace thrive;


struct ComponentRegistry::Implementation {

    std::unordered_map<Component::TypeId, std::string> m_typeIdToName;

    std::unordered_map<std::string, Component::TypeId> m_typeNameToId;

};


ComponentRegistry&
ComponentRegistry::instance() {
    static ComponentRegistry instance;
    return instance;
}


ComponentRegistry::ComponentRegistry()
  : m_impl(new Implementation())
{
}


ComponentRegistry::~ComponentRegistry() {}


bool
ComponentRegistry::registerComponent(
    Component::TypeId typeId,
    const std::string& name
) {
    // Insert name
    auto nameInsertionResult = m_impl->m_typeNameToId.insert(
        std::make_pair(name, typeId)
    );
    if (not nameInsertionResult.second) {
        std::cout << "Duplicate component name: " << name << std::endl;
        return false;
    }
    // Insert type id
    auto idInsertionResult = m_impl->m_typeIdToName.insert(
        std::make_pair(typeId, name)
    );
    if (not idInsertionResult.second) {
        m_impl->m_typeNameToId.erase(nameInsertionResult.first);
        std::cout << "Duplicate component id: " << name << std::endl;
        return false;
    }
    // Success
    return true;
}


Component::TypeId
ComponentRegistry::typeNameToId(
    const std::string& name
) {
    auto iter = m_impl->m_typeNameToId.find(name);
    if (iter == m_impl->m_typeNameToId.end()) {
        throw std::invalid_argument("Component not found: " + name);
    }
    return iter->second;
}


std::string
ComponentRegistry::typeIdToName(
    Component::TypeId typeId
) {
    auto iter = m_impl->m_typeIdToName.find(typeId);
    if (iter == m_impl->m_typeIdToName.end()) {
        throw std::invalid_argument("Component not found: " + boost::lexical_cast<std::string>(typeId));
    }
    return iter->second;
}


