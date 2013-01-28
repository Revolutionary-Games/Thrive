#ifndef WORLD_H
#define	WORLD_H

#include <OgreRoot.h>
#include <OgreEntity.h>

class World
{
    public:
        World(Ogre::SceneManager*);
        virtual ~World(void);
        virtual bool Update(Ogre::Vector3 camNodePosition);
        
    protected:
        bool setBackground(Ogre::String materialName);
        
    private:
        Ogre::SceneManager*     mSceneMgr;
        Ogre::Entity**           mBackgroundEnt;
        Ogre::SceneNode**        mBackgroundNode;
};

#endif	/* WORLD_H */

