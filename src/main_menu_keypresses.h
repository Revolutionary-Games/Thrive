#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace thrive {

//! This detects key presses in the main menu
class MainMenuKeyPressListener : public Leviathan::InputReceiver {
public:
    //! Initializes the key definitions
    MainMenuKeyPressListener();

    //! Detects the important keypresses and notifies ThriveGame
    virtual bool
        ReceiveInput(int32_t key, int modifiers, bool down) override;

    virtual void
        ReceiveBlockedInput(int32_t key, int modifiers, bool down) override;

    virtual bool
        OnMouseMove(int xmove, int ymove) override;

    void
        setEnabled(bool enabled)
    {
        m_enabled = enabled;
    }

private:
    // Set to false when not in the main menu
    bool m_enabled = true;
};

} // namespace thrive
