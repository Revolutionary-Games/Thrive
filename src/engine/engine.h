#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/typedefs.h"
#include "signals/signal.h"

#include <chrono>
#include <memory>
#include <unordered_set>

namespace thrive {

class ComponentCollection;
class EntityManager;

class Engine {

public:

    Engine();

    Engine(const Engine& other) = delete;

    virtual ~Engine() = 0;

    void
    addSystem(
        std::string name,
        System::Order,
        std::shared_ptr<System> system
    );

    std::unordered_set<EntityId>
    entities() const;

    Component*
    getComponent(
        EntityId entityId,
        Component::TypeId typeId
    ) const;

    template<typename ComponentType>
    ComponentType*
    getComponent(
        EntityId entityId
    ) {
        Component* component = this->getComponent(
            entityId,
            ComponentType::TYPE_ID()
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
    init(
        EntityManager* entityManager
    );

    void
    removeSystem(
        std::string name
    );

    void
    setTargetFrameRate(
        unsigned short fps
    );

    virtual void
    shutdown();

    std::chrono::microseconds
    targetFrameDuration() const;

    unsigned short
    targetFrameRate() const;

    virtual void 
    update();

    Signal<EntityId>
    sig_entityAdded;

    Signal<EntityId>
    sig_entityRemoved;

private:
    
    friend class EntityManager;

    void
    addComponent(
        EntityId entityId,
        std::shared_ptr<Component> component
    );

    void
    removeComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


class EngineRunner {

public:

    static EngineRunner*
    current();

    EngineRunner(
        Engine& engine
    );

    ~EngineRunner();

    Engine&
    engine();

    FrameIndex
    currentFrame() const;

    bool isRunning() const;

    void start(
        EntityManager* entityManager
    );

    void stop();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
