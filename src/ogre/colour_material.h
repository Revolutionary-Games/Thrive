#pragma once

namespace Ogre {
    class Material;
    template<class t> class SharedPtr;
    typedef SharedPtr<Material> MaterialPtr;
    class ColourValue;
}

namespace thrive {

Ogre::MaterialPtr
getColourMaterial(
    const Ogre::ColourValue& colour
);

}
