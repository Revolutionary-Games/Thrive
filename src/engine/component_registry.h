#pragma once

#include "engine/component.h"

#include <memory>
#include <stdexcept>

namespace thrive {

/**
* @brief Central registry for component classes
*/
class ComponentRegistry {

public:

    /**
    * @brief Returns the singleton instance
    */
    static ComponentRegistry&
    instance();

    /**
    * @brief Registers a component by class
    *
    * @tparam C
    *   A class derived from Component
    *
    * @return 
    *   \c true if the registration was successful, \c false otherwise
    */
    template<typename C>
    bool
    registerClass() {
        return this->registerComponent(
            C::TYPE_ID(),
            C::TYPE_NAME()
        );
    }

    /**
    * @brief Registers a component type
    *
    * @param typeId
    *   The component's type id
    * @param name
    *   The component's name (e.g. for scripts)
    *
    * @return \c true if the registration was successful, false otherwise
    *
    * @note
    *   Use the REGISTER_COMPONENT macro for easier registration
    */
    bool
    registerComponent(
        Component::TypeId typeId,
        const std::string& name
    );

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
    typeNameToId(
        const std::string& name
    );

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
    typeIdToName(
        Component::TypeId typeId
    );

private:

    ComponentRegistry();

    ~ComponentRegistry();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

/**
 * @brief Registers a component class with the ComponentRegistry
 *
 * Use this in the component's source file.
 */
#define REGISTER_COMPONENT(cls) \
    static const bool cls ## _REGISTERED = thrive::ComponentRegistry::instance().registerClass<cls>();

}
