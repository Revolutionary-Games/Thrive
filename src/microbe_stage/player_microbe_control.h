#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace thrive{

//! Detects player input in the cell stage
class PlayerMicrobeControl : public Leviathan::InputReceiver{
public:

    PlayerMicrobeControl();

    //! Detects the important keypresses and sets the player movement status
    virtual bool ReceiveInput(int32_t key, int modifiers, bool down) override;
    
    virtual void ReceiveBlockedInput(int32_t key, int modifiers, bool down) override;

    virtual bool OnMouseMove(int xmove, int ymove) override;

    void setEnabled(bool enabled){
        m_enabled = enabled;
    }
    
private:

    Leviathan::GKey m_reproduceCheat;

    //! Set to false when not in the microbe stage (or maybe editor as
    //! well could use this) to not send control events
    bool m_enabled = false;
};

}
