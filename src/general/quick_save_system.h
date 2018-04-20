#pragma once

#include "engine/system.h"

namespace sol {
class state;
}

namespace thrive {

/**
 * @brief The quick save mechanism for the game.
 *
 */
class QuickSaveSystem : public System {
public:
    /**
     * @brief Lua bindings
     *
     * Exposes:
     * - QuickSaveSystem()
     *
     * @return
     */
    static void
        luaBindings(sol::state& lua);

    /**
     * @brief Constructor
     */
    QuickSaveSystem();

    /**
     * @brief Initializes the system
     *
     */
    void
        init(GameStateData* gameState) override;

    /**
     * @brief Updates the system
     *
     * @param renderTime
     *
     * @param logicTime
     */
    void
        update(int renderTime, int logicTime) override;

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};
} // namespace thrive
