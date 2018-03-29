#include "player_microbe_control.h"

#include "ThriveGame.h"

#include "engine/player_data.h"

#include "generated/cell_stage_world.h"

#include <Entities/GameWorld.h>
#include <Entities/ScriptComponentHolder.h>
#include <Addons/GameModule.h>
#include <Rendering/GraphicalInputEntity.h>

#include <OgreRay.h>

using namespace thrive;
// ------------------------------------ //
PlayerMicrobeControl::PlayerMicrobeControl() :
	m_reproduceCheat(Leviathan::GKey::GenerateKeyFromString("P")),
	m_forward(Leviathan::GKey::GenerateKeyFromString("W")),
	m_backwards(Leviathan::GKey::GenerateKeyFromString("S")),
	m_left(Leviathan::GKey::GenerateKeyFromString("Q")),
	m_right(Leviathan::GKey::GenerateKeyFromString("E")),
	m_rotateLeft(Leviathan::GKey::GenerateKeyFromString("A")),
	m_rotateRight(Leviathan::GKey::GenerateKeyFromString("D"))
{
    
}
// ------------------------------------ //
bool
PlayerMicrobeControl::ReceiveInput(
    int32_t key, int modifiers, bool down
){
    bool active = down && m_enabled;

    if(handleMovementKeys(key, modifiers, active))
        return active;    
    
    if(!active)
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
    handleMovementKeys(key, modifiers, false);
}

bool
PlayerMicrobeControl::OnMouseMove(
    int xmove, int ymove
){
    return false;
}
// ------------------------------------ //
bool PlayerMicrobeControl::handleMovementKeys(
    int32_t key, int modifiers, bool down
) {

    bool matched = false;

    // This could be made easier by just directly manipulating the
    // vectors but this way mashing all the movement keys will make
    // them cancel out. Not sure which way is better
    
    if(m_forward.Match(key, modifiers)){

        if(down){

            if(!m_forwardActive){

                m_forwardActive = true;
                m_playerMovementVector += Float3(0, 0, -1);
            }
            
        } else {

            if(m_forwardActive){

                m_forwardActive = false;
                m_playerMovementVector -= Float3(0, 0, -1);
            }
        }
        
        matched = true;
        
    } else if(m_backwards.Match(key, modifiers)){

        if(down){

            if(!m_backwardsActive){

                m_backwardsActive = true;
                m_playerMovementVector += Float3(0, 0, 1);
            }
            
        } else {

            if(m_backwardsActive){

                m_backwardsActive = false;
                m_playerMovementVector -= Float3(0, 0, 1);
            }
        }
        
        matched = true;
        
    } else if(m_left.Match(key, modifiers)){

        if(down){

            if(!m_leftActive){

                m_leftActive = true;
                m_playerMovementVector += Float3(-1, 0, 0);
            }
            
        } else {

            if(m_leftActive){

                m_leftActive = false;
                m_playerMovementVector -= Float3(-1, 0, 0);
            }
        }

        matched = true;
        
    } else if(m_right.Match(key, modifiers)){

        if(down){

            if(!m_rightActive){

                m_rightActive = true;
                m_playerMovementVector += Float3(1, 0, 0);
            }
            
        } else {

            if(m_rightActive){

                m_rightActive = false;
                m_playerMovementVector -= Float3(1, 0, 0);
            }
        }

        matched = true;

	} else if (m_rotateLeft.Match(key, modifiers)){
		
		if (down){
			if (!m_rotateLeftActive)
				m_rotateLeftActive = true;
		}
		else {
			if (m_rotateLeftActive)
				m_rotateLeftActive = false;
		}

		matched = true;

	} else if (m_rotateRight.Match(key, modifiers)){
		
		if (down){
			if (!m_rotateRightActive)
				m_rotateRightActive = true;
		}
		else {
			if (m_rotateRightActive)
				m_rotateRightActive = false;
		}

		matched = true;

	}
    
    if(matched){
        return true;
    }
    
    return false;
}
// ------------------------------------ //
// PlayerMicrobeControlSystem
PlayerMicrobeControlSystem::~PlayerMicrobeControlSystem(){
    if(Holder)
        Holder->Release();
}

void PlayerMicrobeControlSystem::Run(
    Leviathan::GameWorld &world
) {
    ObjectID controlledEntity = ThriveGame::Get()->playerData().activeCreature();

    if(controlledEntity == NULL_OBJECT)
        return;
    
    // TODO: just delegate this to a script to avoid the major
    // headaches with getting the properties from c++
    ThriveGame* thrive = ThriveGame::Get();

    Float3 lookPoint;
	bool rotateLeft = thrive->getPlayerInput()->getRotateLeftActive();
	bool rotateRight = thrive->getPlayerInput()->getRotateRightActive();
	if (rotateLeft)
		thrive->getPlayerInput()->rotateLeft();
	if (rotateRight)
		thrive->getPlayerInput()->rotateRight();
	double targetAngle = thrive->getPlayerInput()->getTargetAngle();
    
    try{
        lookPoint = getTargetPoint(world, targetAngle, controlledEntity);
    } catch(const Leviathan::InvalidState& e){

        LOG_ERROR("PlayerMicrobeControlSystem: cannot run because world has no "
            "active camera, exception: ");
        e.PrintToLog();
        return;
    }
    
    const Float3 movementDirection = thrive->getPlayerInput()->getMovement();

    auto module = thrive->getMicrobeScripts();

    if(!module)
        LOG_FATAL("PlayerMicrobeControlSystem: microbe scripts aren't loaded");

    // Debug for movement keys
    // std::stringstream msg;
    // msg << "Input: " << movementDirection << " and look: " << lookPoint;
    // LOG_WRITE(msg.str());

    ScriptRunningSetup setup("applyCellMovementControl");
    auto result = module->ExecuteOnModule<void>(setup, false, &world, controlledEntity,
        movementDirection.Normalize(), lookPoint);

    if(result.Result != SCRIPT_RUN_RESULT::Success){
        LOG_WARNING("PlayerMicrobeControlSystem: failed to Run script "
            "applyCellMovementControl");
    }
}
// ------------------------------------ //
Float3 PlayerMicrobeControlSystem::getTargetPoint(Leviathan::GameWorld &worldWithCamera, double targetAngle, ObjectID controlledEntity){

	double x, y;

	Leviathan::Position& camera = worldWithCamera.GetComponent<Leviathan::Position>(controlledEntity);
	Float3 camera_position = camera.Members._Position;
	x = camera_position.GetX();
	y = camera_position.GetY();
	//So right now in order to avoid rounding errors I'm having to multiply by large numbers, presumably every order of magnitude of distance requires another 0, not ideal
	x += 10000000 * std::cos(targetAngle);
	y += 10000000 * std::sin(targetAngle);

    auto ray = worldWithCamera.CastRayFromCamera(x, y); 
    
    const auto plane = Ogre::Plane(Ogre::Vector3(0, 1, 0), 0);

    bool intersects;
    float distance;
    
    std::tie(intersects, distance) = ray.intersects(plane);

    // TODO: could check that intersects is true
    return ray.getPoint(distance);
}

