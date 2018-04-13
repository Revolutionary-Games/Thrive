#include "main_menu_keypresses.h"

#include "ThriveGame.h"

using namespace thrive;
// ------------------------------------ //
MainMenuKeyPressListener::MainMenuKeyPressListener() :
    m_skipKeys({Leviathan::GKey::GenerateKeyFromString("ESCAPE"),
                Leviathan::GKey::GenerateKeyFromString("RETURN"),
                Leviathan::GKey::GenerateKeyFromString("SPACE")})
{
    
}
// ------------------------------------ //
bool
MainMenuKeyPressListener::ReceiveInput(
    int32_t key, int modifiers, bool down
){
    if(!down || !m_enabled)
        return false;

    // LOG_INFO("Main menu keypress: " + std::to_string(key));

    // Check does it match any skip keys //
    for(const auto& skipKey : m_skipKeys){
        if(skipKey.Match(key, modifiers)){

            LOG_INFO("Intro video skip pressed");
            ThriveGame::Get()->onIntroSkipPressed();
            return true;
        }
    }

    // Not used
    return false;
}
// ------------------------------------ //
void
MainMenuKeyPressListener::ReceiveBlockedInput(
    int32_t key, int modifiers, bool down
){
}

bool
MainMenuKeyPressListener::OnMouseMove(
    int xmove, int ymove
){
    return false;
}

