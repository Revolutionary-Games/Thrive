/* 
 * File:   World.h
 * Author: Nelis
 *
 * Created on 27 January 2013, 22:59
 */

#ifndef WORLD_H
#define	WORLD_H

#include <memory>

#include <OgreRoot.h>
#include <OISEvents.h>
#include <OISInputManager.h>
#include <OISKeyboard.h>
#include <OISMouse.h>

class World : public Ogre::FrameListener
     //  , public OIS::MouseListener, public OIS::KeyListener
{
    public:
        World(Ogre::SceneManager*);
        virtual ~World(void);
    
    protected:
        bool setBackground(Ogre::String materialName);
        
        // OIS Listeners
//        bool mouseMoved( const OIS::MouseEvent &arg );
//        bool mousePressed( const OIS::MouseEvent &arg, OIS::MouseButtonID id );
//        bool mouseReleased( const OIS::MouseEvent &arg, OIS::MouseButtonID id );
//        bool keyPressed( const OIS::KeyEvent &arg );
//        bool keyReleased( const OIS::KeyEvent &arg );

    private:
        Ogre::SceneManager*     mSceneMgr;
        Ogre::Plane             mBackgroundPlane;
};

#endif	/* WORLD_H */

