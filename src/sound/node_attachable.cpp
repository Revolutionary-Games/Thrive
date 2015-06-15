#include "node_attachable.h"

#include <OgreNode.h>
#include <OgreSceneManager.h>
#include <OgreSceneNode.h>

using namespace thrive;

struct NodeAttachable::Implementation : public Ogre::Node::Listener{

    Implementation(NodeAttachable* owner) :
        m_node(NULL), m_privateNode(NULL), m_interface(owner)
    {

    }
    
    ~Implementation(){

        setNode(NULL);
        
        m_privateNode = NULL;
        m_node = NULL;
    }

    void setNode(Ogre::SceneNode* node){

        if(m_privateNode && !m_node){

            m_privateNode->setListener(NULL);

            if(m_node){
            
                m_node->removeAndDestroyChild(m_privateNode);
                
            } else {

                auto sceneManager = m_privateNode->getCreator();
                sceneManager->destroySceneNode(m_privateNode);
            }

            m_privateNode = NULL;
        }
        
        if(node == m_node)
            return;

        // This should always be true when this has been properly attached
        // and both nodes are valid
        if(m_node && m_privateNode){
            
            m_privateNode->setListener(NULL);
            m_node->removeAndDestroyChild(m_privateNode);

            m_privateNode = NULL;
        }
        
        m_node = node;

        if(m_node){

            // Because multiple sounds may be attached to a single node/entity
            // we need to create a child node that we then attach a listener to
            m_privateNode = m_node->createChildSceneNode();

            m_privateNode->setListener(this);
            m_interface->onMoved(m_node);
        }
    }
    
    void
    nodeUpdated(
        const Ogre::Node*
    ) override {

        if(m_node){
            
            m_interface->onMoved(m_node);
        }
    }

    void
    nodeDestroyed(
        const Ogre::Node*
    ) override {

        m_node = NULL;
        m_privateNode = NULL;
    }

    void
    nodeDetached(
        const Ogre::Node*
    ) override {

        m_node = NULL;
    }
    
    Ogre::SceneNode* m_node;
    Ogre::SceneNode* m_privateNode;
    NodeAttachable* m_interface;
};

NodeAttachable::NodeAttachable() : m_impl(new Implementation(this)){

}

NodeAttachable::~NodeAttachable(){

    detachFromNode();
    m_impl.reset();
}

void
NodeAttachable::detachFromNode(){

    m_impl->setNode(NULL);
}

void
NodeAttachable::attachToNode(
    Ogre::SceneNode* node
) {

    m_impl->setNode(node);
}

Ogre::SceneNode*
NodeAttachable::getAttachedNode()
    const
{

    return m_impl->m_node;
}

