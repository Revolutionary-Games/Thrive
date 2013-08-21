#pragma once

namespace Ogre {
    class MaterialPtr;
    class ColourValue;
}

namespace thrive {

Ogre::MaterialPtr
getColourMaterial(
    const Ogre::ColourValue& colour
);

}
