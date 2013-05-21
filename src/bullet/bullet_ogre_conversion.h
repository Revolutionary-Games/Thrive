#include <btBulletDynamicsCommon.h>

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive{


////////////////////////////////////////////////////////////////////////////////
// Bullet to Ogre
////////////////////////////////////////////////////////////////////////////////

inline Ogre::Vector3
bulletToOgre(
    const btVector3& v
) {
    return Ogre::Vector3(v.x(), v.y(), v.z());
}


inline Ogre::Quaternion
bulletToOgre(
    const btQuaternion& q
) {
    return Ogre::Quaternion(q.w(), q.x(), q.y(), q.z());
}

////////////////////////////////////////////////////////////////////////////////
// Ogre to Bullet
////////////////////////////////////////////////////////////////////////////////

inline btVector3
ogreToBullet(
    const Ogre::Vector3& v
) {
    return btVector3(v.x, v.y, v.z);
}


inline btQuaternion
ogreToBullet(
    const Ogre::Quaternion& q
) {
    return btQuaternion(q.x, q.y, q.z, q.w);
}

}
