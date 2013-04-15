#include "RandomAgent.h"

RandomAgent::RandomAgent()
{
    
}
std::string RandomAgent::getType()
{
    return "Random";
}
Ogre::Vector3 RandomAgent::update()
{
    Ogre::Vector3 mVelocity = Ogre::Vector3::ZERO;
    mVelocity.x = Ogre::Math::SymmetricRandom();
    mVelocity.y = Ogre::Math::SymmetricRandom();
    return mVelocity*5;
}

