#include "engine/engine.h"

#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "engine/system.h"
#include "game.h"
#include "util/contains.h"
#include "util/pair_hash.h"

#include <boost/thread.hpp>
#include <chrono>
#include <forward_list>
#include <set>
#include <unordered_map>

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// Engine
////////////////////////////////////////////////////////////////////////////////

struct Engine::Implementation {

    using Clock = std::chrono::high_resolution_clock;

    Implementation(
        Engine& engine
    ) : m_engine(engine)
    {
        m_targetFrameDuration = std::chrono::microseconds(1000000 / m_targetFrameRate);
    }

    ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    ) {
        std::unique_ptr<ComponentCollection>& collection = m_components[typeId];
        if (not collection) {
            collection.reset(new ComponentCollection(typeId));
            collection->registerChangeCallbacks(
                [this] (EntityId entityId, Component&) {
                    m_entities[entityId] += 1;
                },
                [this] (EntityId entityId, Component&) {
                    m_entities[entityId] -= 1;
                    assert(m_entities[entityId] >= 0 && "Removed component from non-existent entity");
                }
            );
        }
        return *collection;
    }

    void
    processSystemsQueue() {
        // Remove systems
        while (not m_systemsToRemove.empty()) {
            std::string name = m_systemsToRemove.front();
            System::Ptr system = m_systems[name];
            system->shutdown();
            for (auto iter = m_activeSystems.begin(); iter != m_activeSystems.end(); ++iter) {
                if (system == iter->second) {
                    m_activeSystems.erase(iter);
                    break;
                }
            }
            m_systems.erase(name);
            m_systemsToRemove.pop_front();
        }
        // Activate systems
        while (not m_systemsToActivate.empty()) {
            std::string name;
            System::Order order;
            std::tie(name, order) = m_systemsToActivate.front();
            System::Ptr system = m_systems[name];
            m_activeSystems.insert(std::make_pair(order, system));
            system->init(&m_engine);
            m_systemsToActivate.pop_front();
        }
    }

    std::unordered_map<
        Component::TypeId, 
        std::unique_ptr<ComponentCollection>
    > m_components;

    Engine& m_engine;

    std::unordered_map<EntityId, int> m_entities;

    std::multimap<System::Order, System::Ptr> m_activeSystems;

    EntityManager* m_entityManager = nullptr;

    FrameIndex m_frameIndex = 0;

    bool m_isInitialized = false;

    Clock::time_point m_lastUpdate;

    std::unordered_map<std::string, System::Ptr> m_systems;

    std::forward_list<
        std::pair<std::string, System::Order>
    > m_systemsToActivate;

    std::forward_list<std::string> m_systemsToRemove;

    std::chrono::microseconds m_targetFrameDuration;

    unsigned short m_targetFrameRate = 60;

};


/**
* @brief Constructor
*/
Engine::Engine() 
  : m_impl(new Implementation(*this))
{
}


/**
* @brief Destructor
*
* @note
*   If the engine is still running, this will assert
*/
Engine::~Engine() {
    assert(not m_impl->m_isInitialized && "Engine still running during destruction");
}


/**
* @brief Adds a component to this engine's collections
*
* The component will be added to the appropriate collection's queue.
*
* @param entityId
*   The component's entity
*
* @param component
*   The component to add
*/
void
Engine::addComponent(
    EntityId entityId,
    std::shared_ptr<Component> component
) {
    Component::TypeId typeId = component->typeId();
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    collection.queueComponentAddition(
        entityId, 
        std::move(component)
    );
}


/**
* @brief Adds a system to this engine
*
* @param name
*   The system's name
* @param order
*   The system's priority in the process queue. The lower, the earlier.
* @param system
*   The system to add
*/
void
Engine::addSystem(
    std::string name,
    System::Order order,
    System::Ptr system
) {
    m_impl->m_systems[name] = system;
    m_impl->m_systemsToActivate.push_front(std::make_pair(name, order));
}


/**
* @brief Returns a set of entities present in the engine
*
* @note
*   Don't call this repeatedly, it's expensive.
*/
std::unordered_set<EntityId>
Engine::entities() const {
    std::unordered_set<EntityId> entities;
    for(auto& pair : m_impl->m_entities) {
        entities.insert(pair.first);
    }
    return entities;
}


/**
* @brief Retrieves a component from this engine's collections
*
* @param entityId
*   The component's entity
* @param typeId
*   The component's type id
*
* @return 
*   A non-owning pointer to the component or \c nullptr if no such
*   component was found
*/
Component*
Engine::getComponent(
    EntityId entityId,
    Component::TypeId typeId
) const {
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    return collection.get(entityId);
}


/**
* @brief Returns a component collection
*
* @param typeId
*   The component type the collection is holding
*
*/
ComponentCollection&
Engine::getComponentCollection(
    Component::TypeId typeId
) const {
    return m_impl->getComponentCollection(typeId);
}


/**
* @brief Returns one of the engine's systems
*
* @param name
*   The system's name
*
* @return 
*   The system or \c nullptr if no such system was found
*/
System::Ptr
Engine::getSystem(
    std::string name
) const {
    auto iter = m_impl->m_systems.find(name);
    if (iter != m_impl->m_systems.end()) {
        return iter->second;
    }
    else {
        return nullptr;
    }
}


/**
* @brief Initializes the engine
*
* Override this in your subclass to initialize data structures
* and add essential systems. 
*
* @warning
*   Do not forget to call Engine::init in your overriding implementation!
*
* @param entityManager
*   The entity manager supplying the engine's entities
*   
*/
void
Engine::init(
    EntityManager* entityManager
) {
    m_impl->m_entityManager = entityManager;
    m_impl->m_isInitialized = true;
    entityManager->registerEngine(this);
    m_impl->m_lastUpdate = Implementation::Clock::now();
}


/**
* @brief Removes a component from this engine's collections
*
* If no such component exists, does nothing.
*
* @param entityId
*   The component's entity
* @param typeId
*   The component's type id
*/
void
Engine::removeComponent(
    EntityId entityId,
    Component::TypeId typeId
) {
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    collection.queueComponentRemoval(entityId);
}


/**
* @brief Removes a system from the engine by name
*
* The system is removed at the beginning of the next frame
*
* If no such system is found, does nothing.
*
* @param name
*   The system's name
*   
*/
void
Engine::removeSystem(
    std::string name
) {
    m_impl->m_systemsToRemove.push_front(name);
}


/**
* @brief Sets the target frame rate
*
* @note
*   Asserts if \c fps is 0.
*
* @param fps
*   The target frame rate in frames per second
*/
void
Engine::setTargetFrameRate(
    unsigned short fps
) {
    assert(fps != 0 && "Can't set a 0 framerate");
    m_impl->m_targetFrameRate = fps;
    m_impl->m_targetFrameDuration = std::chrono::microseconds(1000000 / fps);
}


/**
* @brief Shuts down the engine
*
* Override this in your subclass to unwind the stuff your init function
* has set up.
*
* @warning
*   Do not forget to call Engine::shutdown in your overriding implementation!
*/
void
Engine::shutdown() {
    m_impl->m_entityManager->unregisterEngine(this);
    m_impl->m_entityManager = nullptr;
    m_impl->m_isInitialized = false;
}


/**
* @brief Returns the target frame duration
*/
std::chrono::microseconds
Engine::targetFrameDuration() const {
    return m_impl->m_targetFrameDuration;
}


/**
* @brief Returns the target frame rate in fps
*/
unsigned short
Engine::targetFrameRate() const {
    return m_impl->m_targetFrameRate;
}


/**
* @brief Renders a single frame
*
* The update procedure is as follows:
*
* 1. Process components queued for adding / removing
* 2. Process systems queued for adding / removing
* 3. Iterate through systems in their order, calling System::update on them
*
* Note that this does not include the idling for maintaining the 
* target frame rate.
*/
void
Engine::update() {
    m_impl->m_frameIndex += 1;
    for (auto& pair : m_impl->m_components) {
        pair.second->processQueue();
    }
    m_impl->processSystemsQueue();
    auto now = Implementation::Clock::now();
    auto delta = (now - m_impl->m_lastUpdate);
    int milliSeconds = std::chrono::duration_cast<std::chrono::milliseconds>(delta).count();
    m_impl->m_lastUpdate = now;
    for(auto& value : m_impl->m_activeSystems) {
        value.second->update(milliSeconds);
    }
}


////////////////////////////////////////////////////////////////////////////////
// EngineRunner
////////////////////////////////////////////////////////////////////////////////

struct EngineRunner::Implementation {

    using Clock = std::chrono::high_resolution_clock;

    Implementation(
        EngineRunner& engineRunner,
        Engine& engine
    ) : m_engine(engine),
        m_engineRunner(engineRunner)
    {
    }

    void
    run(
        EntityManager* entityManager
    ) {
        Implementation::threadLocalStorage().reset(&m_engineRunner);
        m_keepRunning = true;
        m_engine.init(entityManager);
        while (m_keepRunning) {
            Clock::time_point start = Clock::now();
            m_engine.update();
            Clock::time_point stop = Clock::now();
            Clock::duration frameDuration = stop - start;
            Clock::duration sleepDuration = m_engine.targetFrameDuration() - frameDuration;
            if (sleepDuration.count() > 0) {
                auto microseconds = std::chrono::duration_cast<std::chrono::microseconds>(sleepDuration).count();
                boost::chrono::microseconds boostDuration = boost::chrono::microseconds(microseconds);
                boost::this_thread::sleep_for(boostDuration);
            }
        }
        m_engine.shutdown();
        Implementation::threadLocalStorage().reset(nullptr);
    }

    Engine& m_engine;

    EngineRunner& m_engineRunner;

    bool m_keepRunning = false;

    std::unique_ptr<boost::thread> m_thread;

    static void cleanup(EngineRunner*) {}

    static boost::thread_specific_ptr<EngineRunner>&
    threadLocalStorage() {
        static boost::thread_specific_ptr<EngineRunner> storage(cleanup);
        return storage;
    }

};


/**
* @brief Constructor
*
* @param engine
*   The engine to run
*/
EngineRunner::EngineRunner(
    Engine& engine
) : m_impl(new Implementation(*this, engine))
{
}


/**
* @brief Destructor
*
* If the engine is still running, stops it
*/
EngineRunner::~EngineRunner() {
    this->stop();
}


/**
* @brief Returns the engine runner of the current thread
*/
EngineRunner*
EngineRunner::current() {
    return Implementation::threadLocalStorage().get();
}


/**
* @brief Returns whether the engine is currently running
*/
bool
EngineRunner::isRunning() const {
    return m_impl->m_thread != nullptr;
}


/**
* @brief Starts the engine
*
* This starts a new thread. In this thread, the following is executed:
*
* 1. Call Engine::init()
* 2. While the engine is kept running, call Engine::update repeatedly
* 3. If necessary, sleep between frames to maintain the engine's
*    target framerate
* 4. When the engine is stopped, call Engine::shutdown()
*
* @param entityManager
*   The entity manager to supply the engine's components
*/
void
EngineRunner::start(
    EntityManager* entityManager
) {
    assert(m_impl->m_thread == nullptr && "Double start of engine");
    m_impl->m_thread.reset(new boost::thread(
        std::bind(&Implementation::run, m_impl.get(), entityManager)
    ));
}


/**
* @brief Stops the engine if it's running
*
* This function blocks until the engine is done rendering its current
* frame.
*/
void
EngineRunner::stop() {
    if (m_impl->m_thread) {
        m_impl->m_keepRunning = false;
        m_impl->m_thread->join();
        m_impl->m_thread.reset();
    }
}
