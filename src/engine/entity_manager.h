#pragma once

#include "engine/entity.h"

#include <memory>

namespace thrive {

class Component;
class Engine;

class EntityManager {

public:

    static EntityManager&
    instance();

    void
    addComponent(
        Entity::Id entityId,
        std::shared_ptr<Component> component
    );

    void
    registerEngine(
        Engine* engine
    );

    void
    removeComponent(
        Entity::Id entityId,
        Component::TypeId typeId
    );

    void
    removeEntity(
        Entity::Id entityId
    );

    void
    unregisterEngine(
        Engine* engine
    );

private:

    EntityManager();

    ~EntityManager();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
