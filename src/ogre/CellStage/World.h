#ifndef WORLD_H
#define	WORLD_H

#include <OgreRoot.h>
#include <OISEvents.h>
#include <OISInputManager.h>
#include <OISKeyboard.h>
#include <OISMouse.h>

class World
{
    public:
        World(Ogre::SceneManager*);
        virtual ~World(void);
    
    protected:
        bool setBackground(Ogre::String materialName);
        
    private:
        Ogre::SceneManager*     mSceneMgr;
        Ogre::Plane             mBackgroundPlane;
};

#endif	/* WORLD_H */

