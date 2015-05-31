#pragma once

namespace Ogre {
#ifdef USE_OGRE2
    class Material;
    template<class t> class SharedPtr;
    typedef SharedPtr<Material> MaterialPtr;
#else
    class MaterialPtr;
#endif //USE_OGRE2
    class ColourValue;
}

namespace thrive {

Ogre::MaterialPtr
getColourMaterial(
    const Ogre::ColourValue& colour
);

}
