#pragma once

#include <memory>

namespace thrive {

class Engine;

/**
* @brief A system can update entities' state
*/
class System {

public:

    /**
    * @brief Typedef for ordering of systems
    *
    * @see Engine::addSystem()
    */
    using Order = int;

    /**
    * @brief Constructor
    */
    System();

    /**
    * @brief Destructor
    */
    virtual ~System() = 0;

    /**
    * @brief The system's engine
    *
    * @return 
    *   The system's engine or \c nullptr if the system hasn't been added to
    *   an engine yet.
    */
    Engine*
    engine() const;

    /**
    * @brief Initializes the system
    *
    * Override this to prepare the system for updating.
    *
    * @param engine
    *   The engine the system belongs to
    */
    virtual void
    init(
        Engine* engine
    );

    /**
    * @brief Shuts the system down
    *
    * Override this to gracefully shut down the system, releasing any
    * resources you might have acquired in init() or during calls to
    * update().
    */
    virtual void
    shutdown();

    /**
    * @brief Updates the system
    *
    * Override this to update the relevant entities' states.
    *
    * @param milliSeconds
    *   The time the last frame took to render
    *
    * @note
    *   If you need to know the time since the last call to \a this system's
    *   update() function, you'll have to measure it yourself.
    */
    virtual void
    update(
        int milliSeconds
    ) = 0;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
