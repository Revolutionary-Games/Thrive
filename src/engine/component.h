/**
* @brief 
*/
#pragma once

#include "engine/typedefs.h"

#include <memory>
#include <string>
#include <unordered_map>

namespace sol {
class state;
}

namespace thrive {

class StorageContainer;

/**
* @brief Base class for components
*
* Components are mainly data classes that "tag" entities with certain
* properties.
*
* Good introductions to entity / component models:
* - <a href="http://piemaster.net/2011/07/entity-component-primer/">Entity Component Primer</a>
* - <a href="http://www.gamasutra.com/blogs/MeganFox/20101208/88590/Game_Engines_101_The_EntityComponent_Model.php">Game Engines 101</a>
* - <a href="http://www.richardlord.net/blog/what-is-an-entity-framework">What is an entity system?</a>
*/
class Component {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    *
    * - Component::typeId()
    * - Component::typeName()
    * - Component::touch()
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Destructor
    */
    virtual ~Component() = 0;

    /**
    * @brief A volatile component is not serialized during a save
    *
    * @return 
    */
    bool
    isVolatile() const;

    /**
    * @brief Loads the component
    *
    * @param storage
    */
    virtual void
    load(
        const StorageContainer& storage
    ) = 0;

    /**
    * @brief The entity this component belongs to
    *
    * @return 
    *   The entity this component belongs to or NULL_ENTITY if this component
    *   has not been added to an entity yet
    */
    EntityId
    owner() const {
        return m_owner;
    }

    /**
    * @brief Sets the volatile flag
    *
    * @param isVolatile
    *
    * @see Component::isVolatile
    */
    void
    setVolatile(
        bool isVolatile
    );


    /**
    * @brief Sets the component's owner
    *
    * Used by the EntityManager
    *
    * @param owner
    */
    void
    setOwner(
        EntityId owner
    ) {
        m_owner = owner;
    }

    /**
    * @brief Serializes the component
    *
    * @return 
    */
    virtual StorageContainer
    storage() const = 0;

    /**
    * @brief The component's type id
    */
    virtual ComponentTypeId
    typeId() const = 0;

    /**
    * @brief The component's type name
    */
    virtual std::string
    typeName() const = 0;

protected:

private:

    bool m_isVolatile = false;

    EntityId m_owner = NULL_ENTITY;

};

}

/**
* @brief Fills in basic functions for derived components
*
* This macro creates the following functions:
* - \c TYPE_ID: Static function that returns the component's type id. The id 
*   is generated automatically the first time this is called.
* - \c typeId: Overrides Component::typeId() and returns the type id
*   returned by \c TYPE_ID.
* - \c TYPE_NAME: Static function that returns \a name as a string. To avoid
*   copying, the string is returned as a const reference to a local static
*   variable.
* - \c typeName: Overrides Component::typeName() and returns the name returned
*   by \c TYPE_NAME.
*
* @param name 
*   The component's name
*
* Example:
* \code
* class MyComponent : public Component {
*     COMPONENT(MyComponent)
* public:
*     // ...
* };
* \endcode
*/
#define COMPONENT(name)  \
    public: \
        \
        static const ComponentTypeId TYPE_ID; \
        \
        ComponentTypeId typeId() const override { \
            return TYPE_ID; \
        } \
        \
        static const std::string& TYPE_NAME() { \
            static std::string string(#name); \
            return string; \
        } \
        \
        std::string typeName() const override { \
            return TYPE_NAME(); \
        } \
        \
    private: \


/**
* @brief Fills in common Lua bindings for derived Components
*
* Uses a faster way to cast than dynamic_cast
*/
#define COMPONENT_BINDINGS(name)                            \
sol::base_classes, sol::bases<Component>(),                 \
"castFrom", [](Component* baseptr){                         \
    if(baseptr->typeId() != name::TYPE_ID)                  \
        return static_cast<name*>(nullptr);                 \
    return static_cast<name*>(baseptr);                     \
},                                                          \
"TYPE_ID", sol::var(name::TYPE_ID),                         \
"TYPE_NAME", &name::TYPE_NAME                               \

