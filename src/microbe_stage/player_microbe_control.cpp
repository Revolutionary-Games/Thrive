#include "player_microbe_control.h"

#include "ThriveGame.h"

using namespace thrive;
// ------------------------------------ //
PlayerMicrobeControl::PlayerMicrobeControl() :
    m_reproduceCheat(Leviathan::GKey::GenerateKeyFromString("P"))
{
    
}
// ------------------------------------ //
bool
PlayerMicrobeControl::ReceiveInput(
    int32_t key, int modifiers, bool down
){
    if(!down || !m_enabled)
        return false;

    LOG_INFO("PMC Key pressed: " + std::to_string(key));

    if(m_reproduceCheat.Match(key, modifiers)){

        LOG_INFO("Reproduce cheat pressed");
        return true;
    }

    // Not used
    return false;
}
// ------------------------------------ //
void
PlayerMicrobeControl::ReceiveBlockedInput(
    int32_t key, int modifiers, bool down
){
}

bool
PlayerMicrobeControl::OnMouseMove(
    int xmove, int ymove
){
    return false;
}

