#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <string>

namespace luabind {
class scope;
}

namespace thrive {

class EntityManager;

class Entity {

public:

    static luabind::scope
    luaBindings();

    Entity();

    Entity(
        EntityManager& entityManager
    );

    Entity(
        EntityId id
    );

    Entity(
        EntityId id,
        EntityManager& entityManager
    );

    Entity(
        const std::string& name
    );

    Entity(
        const std::string& name,
        EntityManager& entityManager
    );

    Entity(
        const Entity& other
    );

    ~Entity();

    Entity&
    operator = (
        const Entity& other
    );

    bool
    operator == (
        const Entity& other
    ) const;

    bool
    exists() const;

    void
    addComponent(
        std::shared_ptr<Component> component
    );

    Component*
    getComponent(
        Component::TypeId typeId
    );

    Component*
    getComponent(
        const std::string& typeName
    );

    bool
    hasComponent(
        Component::TypeId typeId
    );

    bool
    hasComponent(
        const std::string& typeName
    );

    void
    removeComponent(
        Component::TypeId typeId
    );

    void
    removeComponent(
        const std::string& name
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


}
