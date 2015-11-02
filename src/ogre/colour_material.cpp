#include "ogre/colour_material.h"

#include <sstream>
#include <string>
#include <OgreColourValue.h>
#include <OgreMaterial.h>
#include <OgreMaterialManager.h>


static Ogre::String
getColourName(
    const Ogre::ColourValue colour
) {
    std::ostringstream stream;
    stream << colour;
    return stream.str();
}



Ogre::MaterialPtr
thrive::getColourMaterial(
    const Ogre::ColourValue& colour
) {
    Ogre::MaterialManager& manager = Ogre::MaterialManager::getSingleton();
    Ogre::String name = getColourName(colour);
    Ogre::MaterialPtr material = manager.getByName(
        name
    );
    if (material.isNull()) {
        material = manager.getDefaultSettings()->clone(
            name
        );
        material->setAmbient(0.3 * colour);
        material->setDiffuse(colour);
        material->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
    }
    return material;
}
