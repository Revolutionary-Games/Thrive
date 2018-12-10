#include "MicrobeEditorKeyHandler.h"

#include <Application/KeyConfiguration.h>
#include <Engine.h>
#include <Window.h>

using namespace thrive;

MicrobeEditorKeyHandler::MicrobeEditorKeyHandler(KeyConfiguration& keys) :
{}
// ------------------------------------ //
bool
    MicrobeEditorKeyHandler::ReceiveInput(int32_t key, int modifiers, bool down)
{
    if(!down)
        return false;

    // LOG_INFO("Global keypress: " + std::to_string(key));

    /*if(m_screenshot.Match(key, modifiers)) {
        LOG_INFO("Screenshot Time");
        Engine::Get()->SaveScreenShot();
        return true;
    }*/

    // Not used
    return false;
}
// ------------------------------------ //
void
    MicrobeEditorKeyHandler::ReceiveBlockedInput(int32_t key,
        int modifiers,
        bool down)
{}

bool
    MicrobeEditorKeyHandler::OnMouseMove(int xmove, int ymove)
{
    return false;
}
