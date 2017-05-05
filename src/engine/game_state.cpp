#include "engine/game_state.h"

#include "engine/engine.h"

#include "scripting/script_helpers.h"

#include <OgreRoot.h>
#include <OgreSceneManager.h>

using namespace thrive;


void GameStateData::luaBindings(sol::state &lua){

    lua.new_usertype<GameStateData>("GameStateData",

        sol::constructors<sol::types<sol::table, Engine*, EntityManager*,
        PhysicalWorld*>>(),
        
        "luaEngine", &GameStateData::m_engine,
        "name", sol::property(&GameStateData::name)
    );
}

GameStateData::GameStateData(
    sol::table stateObj,
    Engine* engine,
    EntityManager* entityManager,
    PhysicalWorld* physics
) :
    m_engine(engine),
    m_entityManager(entityManager),
    m_physicalWorld(physics),
    m_luaSide(stateObj)
{
    // TODO: configure the number of worker threads, currently always 2
    m_sceneManager = Ogre::Root::getSingleton().createSceneManager(
        Ogre::ST_GENERIC, 2, Ogre::INSTANCING_CULLING_THREADED,
        name()
    );

    // And allow this to be configured, too
    m_sceneManager->setAmbientLight(
        Ogre::ColourValue(0.5, 0.5, 0.5)
    );
}

GameStateData::~GameStateData(){

    // Destroy scene manager
    Ogre::Root* root = Ogre::Root::getSingletonPtr();

    // This object might be destroyed when the Lua state is destroyed so it is not safe
    // to just assume that root is valid
    if(root){
    
        root->destroySceneManager(
            m_sceneManager
        );
    }

    m_sceneManager = nullptr;
}

// ------------------------------------ //

Engine*
GameStateData::engine(){

    return m_engine;
}

EntityManager*
GameStateData::entityManager(){

    return m_entityManager;
}


PhysicalWorld*
GameStateData::physicalWorld(){

    return m_physicalWorld;
}


Ogre::SceneManager*
GameStateData::sceneManager() const{

    return m_sceneManager;
}


std::vector<System*>
GameStateData::getCppSystems(){

    auto result = m_luaSide.get<sol::protected_function>("getCppSystems")(m_luaSide);

    if(!result.valid())
        throw std::runtime_error("GameStateData::getCppSystems failed to "
            "call lua side");

    sol::table systems = result.get<sol::table>();

    return createVectorFromLuaTable<System*>(systems);
}

std::string
GameStateData::name() const{

    return m_luaSide.get<std::string>("name");
}







