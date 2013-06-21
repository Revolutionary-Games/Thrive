#pragma once

#include <memory>

namespace thrive {

class Engine;

/**
* @brief A system handles one specific part of the game
*
* Systems can operate on entities and their components, but they can also 
* handle tasks that don't require components at all, such as issuing a render
* call to the graphics engine.
*/
class System {

public:

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
    *   The system's engine or \c nullptr if the system hasn't been 
    *   initialized yet.
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
    * Override this to update the systems's state.
    *
    * @param milliSeconds
    *   The number of milliseconds to advance
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
