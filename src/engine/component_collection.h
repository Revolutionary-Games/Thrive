#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"
#include "signals/signal.h"

#include <memory>

namespace thrive {

class Engine;

class ComponentCollection {

public:

    ~ComponentCollection();

    Component*
    operator[] (
        EntityId entityId
    ) const;

    Component*
    get(
        EntityId entityId
    ) const;

    Component::TypeId
    type() const;

    mutable Signal<EntityId, Component&>
    sig_componentAdded;

    mutable Signal<EntityId, Component&>
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
        EntityId entityId,
        std::shared_ptr<Component> component
    );

    void
    queueComponentRemoval(
        EntityId entityId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    
};

}

