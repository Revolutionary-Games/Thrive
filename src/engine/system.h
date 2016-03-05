
#pragma once

#include <memory>

namespace luabind {
class scope;
}

namespace thrive {

class Engine;
class EntityManager;
class GameState;

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
    * @brief Lua bindings
    *
    * Exposes:
    * - System::active
    * - System::setActive
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    System();

    /**
    * @brief Destructor
    */
    virtual ~System() = 0;

    /**
    * @brief Called by GameState::activate()
    *
    * Override this if you need to restore some internal state when the
    * system's game state is activated.
    */
    virtual void
    activate();

    /**
    * @brief Called by GameState::deactivate()
    *
    * Override this if you need to clear some internal state when the
    * system's game state is deactivated.
    */
    virtual void
    deactivate();

    /**
    * @brief Whether this system is enabled
    *
    * Disabled systems are not being updated
    *
    * @return
    */
    bool
    enabled() const;

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
    * @brief Returns the system's entity manager
    *
    * If the system has not been initialized yet, this returns \c nullptr.
    *
    */
    EntityManager*
    entityManager() const;

    /**
    * @brief Returns the system's game state
    *
    * If the system has not been initialized yet, this returns \c nullptr.
    */
    GameState*
    gameState() const;

    /**
    * @brief Initializes the system
    *
    * Override this to prepare the system for updating.
    *
    * @param gameState
    *   The gameState the system belongs to
    */
    virtual void
    init(
        GameState* gameState
    );

    /**
    * @brief Initializes the system and gives it a name
    *
    * Override init instead and then call this
    *
    * @param name
    *   The name of the system (for debugging)
    *
    * @param gameState
    *   The gameState the system belongs to
    */
    virtual void
    initNamed(
        std::string name,
        GameState* gameState
    );

    /**
    * @brief Returns system name
    */
    std::string
    getName();

    /**
    * @brief Sets the enabled status of this system
    *
    * Disabled systems are not updated
    *
    * @param enabled
    */
    void
    setEnabled(
        bool enabled
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
    + * @param renderTime
    * The number of milliseconds the frame took to render
    *
    * @param logicTime
    * The number of milliseconds to advance the game logic.
    * The same as render time except when changing gamespeed or pausing.
    *
    * @note
    *   If you need to know the time since the last call to \a this system's
    *   update() function, you'll have to measure it yourself.
    */
    virtual void
    update(
        int renderTime,
        int logicTime
    ) = 0;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
