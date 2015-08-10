#pragma once

#include <memory>

namespace Ogre {

    class SceneNode;
}

namespace cAudio {

    class IListener;
}

namespace thrive {

    class SoundListener final{
public:
        SoundListener(cAudio::IListener* controlledListener);
        ~SoundListener();

        //! @copydoc NodeAttachable::detachFromNode
        void detachFromNode();

        //! @copydoc NodeAttachable::attachToNode
        void attachToNode(Ogre::SceneNode* node);

private:
        
        struct Implementation;
        std::unique_ptr<Implementation> m_impl;
    };

}
