#include "MicrobeEditorKeyHandler.h"

#include "generated/microbe_editor_world.h"
#include "microbe_stage/simulation_parameters.h"

#include <Addons/GameModule.h>
#include <Application/KeyConfiguration.h>
#include <Entities/GameWorld.h>
#include <Entities/ScriptComponentHolder.h>
#include <Window.h>

#include <OgreRay.h>

using namespace thrive;

MicrobeEditorKeyHandler::MicrobeEditorKeyHandler(KeyConfiguration& keys) :
    m_rotateRight(keys.ResolveControlNameToFirstKey("RotateRight")),
    m_rotateLeft(keys.ResolveControlNameToFirstKey("RotateLeft"))
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
