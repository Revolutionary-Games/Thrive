#ifndef CELL_H
#define	CELL_H

#include <OgreRoot.h>
#include <OgreEntity.h>


class Cell  : public Ogre::FrameListener
{
    public:
        Cell(Ogre::SceneManager*, Ogre::Vector3);
        ~Cell(void);
        Ogre::SceneNode*        mNode;
        virtual bool Update(Ogre::Real deltaTime);

    protected:
        // Ogre::FrameListener
        //virtual bool frameRenderingQueued(const Ogre::FrameEvent& evt);
           
    private:
        Ogre::Entity*           mEntity;  
        Ogre::Vector3           mVelocity;
};


#endif	/* CELL_H */

