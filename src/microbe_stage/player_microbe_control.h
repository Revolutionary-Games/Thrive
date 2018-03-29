#pragma once

#include <Input/InputController.h>
#include <Input/Key.h>

namespace Leviathan{
class ScriptComponentHolder;
}

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

	inline void rotateLeft(){
		m_targetAngle -= m_rotateRate;
	}

	inline void rotateRight(){
		m_targetAngle += m_rotateRate;
	}

    inline Float3 getMovement() const{
        return m_playerMovementVector;
    }

	inline double getTargetAngle() const{
		return m_targetAngle;
	}

	inline bool getRotateLeftActive() const{
		return m_rotateLeftActive;
	}

	inline bool getRotateRightActive() const{
		return m_rotateRightActive;
	}

private:

    //! \brief Handles the movement keys as they need to properly get hte blocked events
    //! \returns True if key matched a movement key (even if down is false)
    bool handleMovementKeys(int32_t key, int modifiers, bool down);
    
private:

    Leviathan::GKey m_reproduceCheat;

    Leviathan::GKey m_forward;
    Leviathan::GKey m_backwards;
    Leviathan::GKey m_left;
    Leviathan::GKey m_right;
	Leviathan::GKey m_rotateLeft;
	Leviathan::GKey m_rotateRight;

    bool m_forwardActive = false;
    bool m_backwardsActive = false;
    bool m_leftActive = false;
    bool m_rightActive = false;
	bool m_rotateLeftActive = false;
	bool m_rotateRightActive = false;

	//! \the absolute angle the microbe will try to turn towards
	double m_targetAngle = 0;

	//! \rate at which the microbe turns; 
	const double m_rotateRate = 0.0625;

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

    void Run(
        Leviathan::GameWorld &world
    );

    // Helpers moved from the lua code to here
    //! Computes the point the mouse cursor is at
    static Float3 getTargetPoint(Leviathan::GameWorld &worldWithCamera, double targetAngle, ObjectID controlledEntity);
    
private:
    
    Leviathan::ScriptComponentHolder* Holder = nullptr;
};

}
