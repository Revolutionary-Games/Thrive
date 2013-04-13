#pragma once

#include "engine/entity.h"
#include "signals/signal.h"

#include <memory>

namespace thrive {

class ComponentCollection;
class System;

class Engine {

public:

    Engine();

    virtual ~Engine() = 0;

    void
    addComponent(
        Entity::Id entityId,
        std::unique_ptr<Component> component
    );

    void
    addSystem(
        std::string name,
        std::shared_ptr<System> system
    );

    Component*
    getComponent(
        Entity::Id entityId,
        Component::TypeId typeId
    ) const;

    const ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    );

    std::shared_ptr<System>
    getSystem(
        std::string key
    ) const;

    virtual void
    init() = 0;

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
    removeSystem(
        std::string name
    );

    virtual void
    shutdown();

    void 
    update();

    Signal<Entity::Id>
    sig_entityAdded;

    Signal<Entity::Id>
    sig_entityRemoved;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
