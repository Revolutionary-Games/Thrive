#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace Leviathan {
class ScriptComponentHolder;
}

namespace thrive {

class MicrobeEditorWorld;

//! This detects key presses everywhere
class MicrobeEditorKeyHandler : public Leviathan::InputReceiver {
public:
    //! Initializes the key definitions
    MicrobeEditorKeyHandler(KeyConfiguration& keys);

    //! Detects the important keypresses and does things like taking screenshots
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
    Leviathan::GKey m_rotateRight;
    Leviathan::GKey m_rotateLeft;
    // Set to false when not in the editor
    bool m_enabled = true;
};

} // namespace thrive
