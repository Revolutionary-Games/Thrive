#include "KeyboardAgent.h"
#include <boost/lexical_cast.hpp>

KeyboardAgent::KeyboardAgent(OIS::Keyboard* mKeyboard)
{
    this->mKeyboard = mKeyboard;
    
}
std::string KeyboardAgent::getType()
{
    return "Keyboard";
}
Ogre::Vector3 KeyboardAgent::update()
{
    Ogre::Vector3 Move = Ogre::Vector3::ZERO;
    MessageBox( NULL, "before if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_A)){
        Move += Ogre::Vector3::NEGATIVE_UNIT_X;}
    MessageBox( NULL, "after 1 if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_D)){
        Move += Ogre::Vector3::UNIT_X;}
    MessageBox( NULL, "after 2 if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_W)){
        Move += Ogre::Vector3::UNIT_Y;}
    MessageBox( NULL, "after 3 if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_S)){
        Move += Ogre::Vector3::NEGATIVE_UNIT_Y;}
    MessageBox( NULL, "after 4 if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_R)){
        Move += Ogre::Vector3::NEGATIVE_UNIT_Z;}
    MessageBox( NULL, "after 5 if", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
    if(mKeyboard->isKeyDown(OIS::KC_F)){
        Move += Ogre::Vector3::UNIT_Z;}
    MessageBox( NULL, "after keydowns", "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);
            
    MessageBox( NULL, boost::lexical_cast<std::string>(Move).c_str(), "nodePosition", MB_OK | MB_ICONERROR | MB_TASKMODAL);

    return Move;
}
