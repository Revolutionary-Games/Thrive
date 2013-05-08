#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <memory>

namespace thrive {

class Component;
class Engine;

class EntityManager {

public:

    static const EntityId NULL_ID;

    EntityManager();

    ~EntityManager();

    void
    addComponent(
        EntityId entityId,
        std::shared_ptr<Component> component
    );

    void
    clear();

    EntityId
    generateNewId();

    Component*
    getComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    EntityId
    getNamedId(
        const std::string& name
    );

    bool
    exists(
        EntityId
    ) const;

    void
    registerEngine(
        Engine* engine
    );

    void
    removeComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    void
    removeEntity(
        EntityId entityId
    );

    void
    unregisterEngine(
        Engine* engine
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
