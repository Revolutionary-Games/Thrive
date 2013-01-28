#ifndef CELL_H
#define	CELL_H

#include <OgreRoot.h>
#include <OgreEntity.h>


class Cell  : public Ogre::FrameListener
{
    public:
        Cell(Ogre::SceneManager*, Ogre::Vector3);
        ~Cell(void);

    protected:
        // Ogre::FrameListener
        virtual bool frameRenderingQueued(const Ogre::FrameEvent& evt);
        virtual bool Update(Ogre::Real deltaTime);
        
    private:
        Ogre::Entity*           mEntity;
        Ogre::SceneNode*        mNode;
        Ogre::Vector3           mVelocity;
};


#endif	/* CELL_H */

