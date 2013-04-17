#pragma once

#include "engine/entity.h"
#include "signals/signal.h"

#include <memory>

namespace thrive {

class Engine;

class ComponentCollection {

public:

    ~ComponentCollection();

    Component*
    operator[] (
        Entity::Id entityId
    ) const;

    Component*
    get(
        Entity::Id entityId
    ) const;

    Component::TypeId
    type() const;

    mutable Signal<Entity::Id, Component&>
    sig_componentAdded;

    mutable Signal<Entity::Id, Component&>
    sig_componentRemoved;

private:

    friend class Engine;

    ComponentCollection(
        Component::TypeId type
    );

    void
    processQueue();

    void
    queueComponentAddition(
        Entity::Id entityId,
        std::shared_ptr<Component> component
    );

    void
    queueComponentRemoval(
        Entity::Id entityId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    
};

}

