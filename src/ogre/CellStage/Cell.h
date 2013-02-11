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
        Ogre::Vector3           mVelocity;
        Ogre::Entity*           mEntity;  

    protected:
        // Ogre::FrameListener
        //virtual bool frameRenderingQueued(const Ogre::FrameEvent& evt);
           
    private:
        
};


#endif	/* CELL_H */

