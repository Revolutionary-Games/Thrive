#pragma once

#include <memory>

namespace Ogre {

    class SceneNode;
}

namespace thrive{

/**
   @brief A helper class for sound objects that want to move with Ogre::SceneNode
*/
class NodeAttachable{
public:

    NodeAttachable();
    ~NodeAttachable();
    
    /**
       @brief Detaches this from a scene node, stops this from moving with the node
    */
    void detachFromNode();

    /**
       @brief Attaches this object to an Ogre SceneNode and makes this move with the node
    */
    void attachToNode(Ogre::SceneNode* node);

    //! @brief Returns the ogre node
    Ogre::SceneNode* getAttachedNode() const;
    
protected:

    //! @brief Called when node callback has updated position and node graph has been updated
    virtual void
    onMoved(Ogre::SceneNode* node) = 0;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
