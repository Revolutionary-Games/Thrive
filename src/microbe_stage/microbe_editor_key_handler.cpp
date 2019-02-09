#include "microbe_editor_key_handler.h"

#include <Application/KeyConfiguration.h>
#include <Engine.h>
#include <Events/Event.h>
#include <Events/EventHandler.h>

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


    if(m_rotateRight.Match(key, modifiers)) {
        LOG_INFO("Right pressed");
        Engine::Get()->GetEventHandler()->CallEvent(
            new Leviathan::GenericEvent("PressedRightRotate"));
        return true;
    } else if(m_rotateLeft.Match(key, modifiers)) {
        LOG_INFO("Left pressed");
        Engine::Get()->GetEventHandler()->CallEvent(
            new Leviathan::GenericEvent("PressedLeftRotate"));
        return true;
    }
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
