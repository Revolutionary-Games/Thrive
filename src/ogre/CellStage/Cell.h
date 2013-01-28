#ifndef CELL_H
#define	CELL_H

#include <OgreRoot.h>
#include <OgreEntity.h>


class Cell  : public Ogre::FrameListener
{
    public:
        Cell(Ogre::SceneManager*);
        ~Cell(void);
        
    protected:
        virtual bool Init();
        // Ogre::FrameListener
        virtual bool frameRenderingQueued(const Ogre::FrameEvent& evt);

    private:
        Ogre::Entity*   mEntity;
        Ogre::SceneNode* mNode;
        Ogre::Vector3   mPosition;
};


#endif	/* CELL_H */

