#include "general/global_keypresses.h"

#include <Application/KeyConfiguration.h>
#include <Engine.h>
#include <Window.h>

using namespace thrive;

GlobalUtilityKeyHandler::GlobalUtilityKeyHandler(KeyConfiguration& keys) :
    m_screenshot(keys.ResolveControlNameToFirstKey("Screenshot"))
{
}
// ------------------------------------ //
bool
    GlobalUtilityKeyHandler::ReceiveInput(int32_t key, int modifiers, bool down)
{
    if(!down)
        return false;

    // LOG_INFO("Global keypress: " + std::to_string(key));

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
