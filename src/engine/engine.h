#pragma once

#include "engine/entity.h"
#include "signals/signal.h"

#include <memory>
#include <unordered_set>

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

    std::unordered_set<Entity::Id>
    entities() const;

    Component*
    getComponent(
        Entity::Id entityId,
        Component::TypeId typeId
    ) const;

    template<typename ComponentType>
    ComponentType*
    getComponent(
        Entity::Id entityId
    ) {
        Component* component = this->getComponent(
            entityId,
            ComponentType::TYPE_ID
        );
        return dynamic_cast<ComponentType*>(component);
    }

    const ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    ) const;

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
