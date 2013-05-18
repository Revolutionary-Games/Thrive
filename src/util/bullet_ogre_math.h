#include <btBulletDynamicsCommon.h>

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive{

static Ogre::Vector3 btToOgVector3(btVector3)__attribute__ ((unused));
static Ogre::Quaternion btToOgQuaternion(btQuaternion q) __attribute__ ((unused));
static btVector3 ogToBtVector3(Ogre::Vector3 v)__attribute__ ((unused));
static btQuaternion ogToBtQuaternion(Ogre::Quaternion q)__attribute__ ((unused));

static Ogre::Vector3 btToOgVector3(btVector3 v){
    Ogre::Vector3 vReturn;
    vReturn.x = v.x();
    vReturn.y = v.y();
    vReturn.z = v.z();
    return vReturn;
}

static Ogre::Quaternion btToOgQuaternion(btQuaternion q){
    Ogre::Quaternion qReturn;
    qReturn.x = q.x();
    qReturn.y = q.y();
    qReturn.z = q.z();
    qReturn.w = q.w();
    return qReturn;
}

static btVector3 ogToBtVector3(Ogre::Vector3 v){
    return btVector3{v.x,v.y,v.z};
}

static btQuaternion ogToBtQuaternion(Ogre::Quaternion q){
    return btQuaternion{q.x,q.y,q.z,q.w};
}

}
