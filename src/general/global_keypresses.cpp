// ------------------------------------ //
#include "general/global_keypresses.h"

#include "ThriveGame.h"

#include <Application/KeyConfiguration.h>
#include <Engine.h>
#include <Window.h>
using namespace thrive;
// ------------------------------------ //

GlobalUtilityKeyHandler::GlobalUtilityKeyHandler(KeyConfiguration& keys) :
    m_screenshot(keys.ResolveControlNameToFirstKey("Screenshot")),
    m_debugOverlay(keys.ResolveControlNameToFirstKey("ToggleDebugOverlay")),
    m_debugPhysics(keys.ResolveControlNameToFirstKey("ToggleDebugPhysics"))
{}
// ------------------------------------ //
bool
    GlobalUtilityKeyHandler::ReceiveInput(int32_t key, int modifiers, bool down)
{
    if(!down)
        return false;

    // LOG_INFO("Global keypress: " + std::to_string(key));

    if(m_screenshot.Match(key, modifiers)) {
        LOG_INFO("Screenshot key pressed");
        Engine::Get()->SaveScreenShot();
        return true;
    }

    if(m_debugOverlay.Match(key, modifiers)) {
        if(ThriveGame::Get()) {
            ThriveGame::Get()->toggleDebugOverlay();
        } else {
            LOG_WARNING("Can't toggle debug overlay");
        }
        return true;
    }

    if(m_debugPhysics.Match(key, modifiers)) {
        if(ThriveGame::Get()) {
            ThriveGame::Get()->toggleDebugPhysics();
        } else {
            LOG_WARNING("Can't toggle debug physics");
        }
        return true;
    }

    // Not used
    return false;
}
// ------------------------------------ //
void
    GlobalUtilityKeyHandler::ReceiveBlockedInput(int32_t key,
        int modifiers,
        bool down)
{}

bool
    GlobalUtilityKeyHandler::OnMouseMove(int xmove, int ymove)
{
    return false;
}
