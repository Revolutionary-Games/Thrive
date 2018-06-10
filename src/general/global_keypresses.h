#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace thrive {

//! This detects key presses everywhere
class GlobalUtilityKeyHandler : public Leviathan::InputReceiver {
public:
    //! Initializes the key definitions
    GlobalUtilityKeyHandler(KeyConfiguration& keys);

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
    Leviathan::GKey m_screenshot;

    // Set to false when not in the main menu
    bool m_enabled = true;
};

} // namespace thrive
#pragma once
