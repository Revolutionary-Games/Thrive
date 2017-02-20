#include "engine/game_state.h"

#include <OgreRoot.h>
#include <OgreSceneManager.h>

using namespace thrive;


GameStateData::GameStateData(
    sol::table stateObj,
    LuaEngine* luaEngine) :
    m_engine(luaEngine)
{
    // TODO: configure the number of worker threads, currently always 2
    m_sceneManager = Ogre::Root::getSingleton().createSceneManager(
        Ogre::ST_GENERIC, 2, Ogre::INSTANCING_CULLING_THREADED,
        stateObj.get<std::string>("name")
    );

    // And allow this to be configured, too
    m_sceneManager->setAmbientLight(
        Ogre::ColourValue(0.5, 0.5, 0.5)
    );
}

GameStateData::~GameStateData(){

    // Destroy scene manager
    Ogre::Root::getSingleton().destroySceneManager(
        m_sceneManager
    );

    m_sceneManager = nullptr;
}


