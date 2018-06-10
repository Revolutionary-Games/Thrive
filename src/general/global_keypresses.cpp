#include "general/global_keypresses.h"

#include "ThriveGame.h"
#include <Application/KeyConfiguration.h>
#include <Window.h>

#include <OgreRay.h>

using namespace thrive;

GlobalUtilityKeyHandler::GlobalUtilityKeyHandler(KeyConfiguration& keys) :
    m_screenshot(keys.ResolveControlNameToFirstKey("Screenshot"))
{
}
// ------------------------------------ //
bool
    GlobalUtilityKeyHandler::ReceiveInput(int32_t key, int modifiers, bool down)
{
    if(!down || !m_enabled)
        return false;

    LOG_INFO("Global keypress: " + std::to_string(key));

    if(m_screenshot.Match(key, modifiers)) {
        LOG_INFO("Screenshot Time");
        Engine::Get()->SaveScreenShot();
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
{
}

bool
    GlobalUtilityKeyHandler::OnMouseMove(int xmove, int ymove)
{
    return false;
}
