#include "engine/component_factory.h"

#include <assert.h>
#include <stdexcept>
#include <unordered_map>

#include <iostream>

using namespace thrive;


struct ComponentFactory::Implementation {

    std::unordered_map<Component::TypeId, ComponentConstructor> m_constructors;

    std::unordered_map<Component::TypeId, std::string> m_typeIdToName;

    std::unordered_map<std::string, Component::TypeId> m_typeNameToId;

};


/**
* @brief Returns the singleton instance
*
*/
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


/**
* @brief Creates a component by name
*
* @param name The component's type name
*
* @return A new component
*
* @throws std::invalid_argument if the name is unknown
*
* @note The type id overload should be preferred for performance reasons
*/
std::shared_ptr<Component>
ComponentFactory::create(
    const std::string& name
) {
    Component::TypeId typeId = this->typeNameToId(name);
    return this->create(typeId);
}


/**
* @brief Creates a component by type id
*
* @param name The component's type id
*
* @return A new component
*
* @throws std::invalid_argument if the id is unknown
*/
std::shared_ptr<Component>
ComponentFactory::create(
    Component::TypeId typeId
) {
    auto iter = m_impl->m_constructors.find(typeId);
    if (iter == m_impl->m_constructors.end()) {
        throw std::invalid_argument("Component not found: " + std::to_string(typeId));
    }
    return iter->second();
}


/**
* @brief Registers a component type
*
* @param typeId
*   The component's type id
* @param name
*   The component's name (e.g. for scripts)
* @param constructor
*   An \c std::function taking \c void and returning a shared
*   pointer to a new component.
*
* @return \c true if the registration was successful, false otherwise
*
* @note
*   Use the REGISTER_COMPONENT macro for easier registration
*/
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


/**
* @brief Converts a component type name to the type id
*
* @param name
*   The component type name
*
* @return 
*   The component type id
*
* @throws
*   std::invalid_argument if the name could not be found
*/
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


/**
* @brief Converts a component type id to the type name
*
* @param typeId
*   The component type id
*
* @return 
*   The component type name
*
* @throws
*   std::invalid_argument if the typeId could not be found
*/
std::string
ComponentFactory::typeIdToName(
    Component::TypeId typeId
) {
    auto iter = m_impl->m_typeIdToName.find(typeId);
    if (iter == m_impl->m_typeIdToName.end()) {
        throw std::invalid_argument("Component not found: " + std::to_string(typeId));
    }
    return iter->second;
}




