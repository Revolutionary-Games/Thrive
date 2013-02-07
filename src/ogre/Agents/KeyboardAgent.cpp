#include "KeyboardAgent.h"

KeyboardAgent::KeyboardAgent(Ogre::Vector3 &move)
{
    this->move = move;
}
std::string KeyboardAgent::getType()
{
    return "Keyboard";
}
Ogre::Vector3 KeyboardAgent::update()
{/* Ogre::Vector3 Move = Ogre::Vector3::ZERO;
    if(mKeyboard->isKeyDown(OIS::KC_A))
        Move += Ogre::Vector3::NEGATIVE_UNIT_X;
    if(mKeyboard->isKeyDown(OIS::KC_D))
        Move += Ogre::Vector3::UNIT_X;
    if(mKeyboard->isKeyDown(OIS::KC_W))
        Move += Ogre::Vector3::UNIT_Y;
    if(mKeyboard->isKeyDown(OIS::KC_S))
        Move += Ogre::Vector3::NEGATIVE_UNIT_Y;
    if(mKeyboard->isKeyDown(OIS::KC_R))
        Move += Ogre::Vector3::NEGATIVE_UNIT_Z;
    if(mKeyboard->isKeyDown(OIS::KC_F))
        Move += Ogre::Vector3::UNIT_Z;*/
    return Ogre::Vector3::ZERO;
}
