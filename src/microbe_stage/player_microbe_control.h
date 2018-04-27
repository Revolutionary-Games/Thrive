#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace Leviathan {
class ScriptComponentHolder;
}

namespace thrive {

//! Detects player input in the cell stage
class PlayerMicrobeControl : public Leviathan::InputReceiver {
public:
    PlayerMicrobeControl(KeyConfiguration& keys);

    //! Detects the important keypresses and sets the player movement status
    virtual bool
        ReceiveInput(int32_t key, int modifiers, bool down) override;

    virtual void
        ReceiveBlockedInput(int32_t key, int modifiers, bool down) override;

    virtual bool
        OnScroll(int x, int y, int modifiers) override;

    virtual bool
        OnMouseMove(int xmove, int ymove) override;

    void
        setEnabled(bool enabled)
    {
        m_enabled = enabled;
    }

    inline Float3
        getMovement() const
    {
        return m_playerMovementVector;
    }

private:
    //! \brief Handles the movement keys as they need to properly get hte
    //! blocked events \returns True if key matched a movement key (even if down
    //! is false)
    bool
        handleMovementKeys(int32_t key, int modifiers, bool down);

private:
    Leviathan::GKey m_reproduceCheat;
    Leviathan::GKey m_engulfMode;

    Leviathan::GKey m_forward;
    Leviathan::GKey m_backwards;
    Leviathan::GKey m_left;
    Leviathan::GKey m_right;

    std::vector<Leviathan::GKey> m_zoomIn;
    std::vector<Leviathan::GKey> m_zoomOut;

    bool m_forwardActive = false;
    bool m_backwardsActive = false;
    bool m_leftActive = false;
    bool m_rightActive = false;


    //! Set to false when not in the microbe stage (or maybe editor as
    //! well could use this) to not send control events
    bool m_enabled = false;

    //! Keeps track of the direction the player wants to go
    Float3 m_playerMovementVector = Float3(0, 0, 0);
};

//! A system that manages setting the wanted movement on the player's cell
class PlayerMicrobeControlSystem {
public:
    ~PlayerMicrobeControlSystem();

    void
        Run(Leviathan::GameWorld& world);

    // Helpers moved from the lua code to here
    //! Computes the point the mouse cursor is at
    static Float3
        getTargetPoint(Leviathan::GameWorld& worldWithCamera);

private:
    Leviathan::ScriptComponentHolder* Holder = nullptr;
};

} // namespace thrive
