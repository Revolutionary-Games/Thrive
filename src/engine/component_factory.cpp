#include "engine/component_factory.h"

#include <assert.h>
#include <boost/lexical_cast.hpp>
#include <stdexcept>
#include <unordered_map>

#include <iostream>

using namespace thrive;


struct ComponentFactory::Implementation {

    std::unordered_map<Component::TypeId, ComponentConstructor> m_constructors;

    std::unordered_map<Component::TypeId, std::string> m_typeIdToName;

    std::unordered_map<std::string, Component::TypeId> m_typeNameToId;

};


ComponentFactory&
ComponentFactory::instance() {
    static ComponentFactory instance;
    return instance;
}


ComponentFactory::ComponentFactory()
  : m_impl(new Implementation())
{
}


ComponentFactory::~ComponentFactory() {}


std::shared_ptr<Component>
ComponentFactory::create(
    const std::string& name
) {
    Component::TypeId typeId = this->typeNameToId(name);
    return this->create(typeId);
}


std::shared_ptr<Component>
ComponentFactory::create(
    Component::TypeId typeId
) {
    auto iter = m_impl->m_constructors.find(typeId);
    if (iter == m_impl->m_constructors.end()) {
        throw std::invalid_argument("Component not found: " + boost::lexical_cast<std::string>(typeId));
    }
    return iter->second();
}


bool
ComponentFactory::registerComponent(
    Component::TypeId typeId,
    const std::string& name,
    ComponentConstructor constructor
) {
    std::cout << "Registering " << name << std::endl;
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
    // Insert constructor
    auto constructorInsertionResult = m_impl->m_constructors.insert(
        std::make_pair(typeId, constructor)
    );
    assert(constructorInsertionResult.second && "Name and type id are unique, but constructor is not?!");
    return true;
}


Component::TypeId
ComponentFactory::typeNameToId(
    const std::string& name
) {
    auto iter = m_impl->m_typeNameToId.find(name);
    if (iter == m_impl->m_typeNameToId.end()) {
        throw std::invalid_argument("Component not found: " + name);
    }
    return iter->second;
}


std::string
ComponentFactory::typeIdToName(
    Component::TypeId typeId
) {
    auto iter = m_impl->m_typeIdToName.find(typeId);
    if (iter == m_impl->m_typeIdToName.end()) {
        throw std::invalid_argument("Component not found: " + boost::lexical_cast<std::string>(typeId));
    }
    return iter->second;
}




