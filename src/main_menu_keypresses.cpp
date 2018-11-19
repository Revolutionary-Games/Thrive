#include "main_menu_keypresses.h"

#include "ThriveGame.h"

using namespace thrive;
// ------------------------------------ //
MainMenuKeyPressListener::MainMenuKeyPressListener() {}
// ------------------------------------ //
bool
    MainMenuKeyPressListener::ReceiveInput(int32_t key,
        int modifiers,
        bool down)
{
    if(!down || !m_enabled)
        return false;

    // LOG_INFO("Main menu keypress: " + std::to_string(key));

    // Not used
    return false;
}
// ------------------------------------ //
void
    MainMenuKeyPressListener::ReceiveBlockedInput(int32_t key,
        int modifiers,
        bool down)
{}

bool
    MainMenuKeyPressListener::OnMouseMove(int xmove, int ymove)
{
    return false;
}
