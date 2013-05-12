#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/typedefs.h"

#include <chrono>
#include <memory>
#include <unordered_set>

namespace thrive {

class ComponentCollection;
class EntityManager;

/**
* @brief An engine with a single purpose
*
* The game consists of several engines. At the time of this writing, they
* are the graphics engine and the script engine. Each engine runs in its
* own thread.
*/
class Engine {

public:

    /**
    * @brief Constructor
    */
    Engine();

    /**
    * @brief Non-copyable
    *
    */
    Engine(const Engine& other) = delete;

    /**
    * @brief Destructor
    *
    * @note
    *   If the engine is still running, this will terminate the application
    */
    virtual ~Engine() = 0;

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
    addSystem(
        std::string name,
        System::Order,
        std::shared_ptr<System> system
    );

    /**
    * @brief Returns a set of entities present in the engine
    *
    * @note
    *   Don't call this repeatedly, it's expensive.
    */
    std::unordered_set<EntityId>
    entities() const;

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
    getComponent(
        EntityId entityId,
        Component::TypeId typeId
    ) const;

    /**
    * @brief Convenience overload
    *
    * @tparam ComponentType
    *   The class of the component to retrieve
    * @param entityId
    *   The component's entity
    *   
    * @return 
    *   A non-owning pointer to the component or \c nullptr if no such
    *   component was found
    */
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

    /**
    * @brief Returns a component collection
    *
    * @param typeId
    *   The component type the collection is holding
    *
    */
    ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    ) const;

    /**
    * @brief Returns one of the engine's systems
    *
    * @param name
    *   The system's name
    *
    * @return 
    *   The system or \c nullptr if no such system was found
    */
    std::shared_ptr<System>
    getSystem(
        std::string name
    ) const;

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
    virtual void
    init(
        EntityManager* entityManager
    );

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
    removeSystem(
        std::string name
    );

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
    setTargetFrameRate(
        unsigned short fps
    );

    /**
    * @brief Shuts down the engine
    *
    * Override this in your subclass to unwind the stuff your init function
    * has set up.
    *
    * @warning
    *   Do not forget to call Engine::shutdown in your overriding implementation!
    */
    virtual void
    shutdown();

    /**
    * @brief Returns the target frame duration
    */
    std::chrono::microseconds
    targetFrameDuration() const;

    /**
    * @brief Returns the target frame rate in fps
    */
    unsigned short
    targetFrameRate() const;

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
    virtual void 
    update();

private:
    
    friend class EntityManager;

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
    addComponent(
        EntityId entityId,
        std::shared_ptr<Component> component
    );

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
    removeComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


/**
* @brief 
*   Handles initialization, updating and shutdown of a single engine in 
*   a separate thread.
*/
class EngineRunner {

public:

    /**
    * @brief Returns the engine runner of the current thread
    */
    static EngineRunner*
    current();

    /**
    * @brief Constructor
    *
    * @param engine
    *   The engine to run
    */
    EngineRunner(
        Engine& engine
    );

    /**
    * @brief Destructor
    *
    * If the engine is still running, stops it
    */
    ~EngineRunner();

    /**
    * @brief Returns whether the engine is currently running
    */
    bool isRunning() const;

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
    void start(
        EntityManager* entityManager
    );

    /**
    * @brief Stops the engine if it's running
    *
    * This function blocks until the engine is done rendering its current
    * frame.
    */
    void stop();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
