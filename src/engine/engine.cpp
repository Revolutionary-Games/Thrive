#include "engine/engine.h"

#include "engine/component_collection.h"
#include "engine/component_factory.h"
#include "engine/entity.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "engine/system.h"
#include "engine/rng.h"
#include "engine/player_data.h"
#include "game.h"

// Bullet
#include "bullet/bullet_to_ogre_system.h"
#include "bullet/rigid_body_system.h"
#include "bullet/update_physics_system.h"
#include "bullet/collision_system.h"

// CEGUI
#include <CEGUI/CEGUI.h>
#include <CEGUI/InputAggregator.h>
#include "CEGUI/RendererModules/Ogre/Renderer.h"
#include "gui/AlphaHitWindow.h"

// Ogre
#include "ogre/camera_system.h"
#include "ogre/keyboard.h"
#include "ogre/light_system.h"
#include "ogre/mouse.h"
#include "ogre/render_system.h"
#include "ogre/scene_node_system.h"
#include "ogre/sky_system.h"

// FFMPEG Initialization
#include "gui/VideoPlayer.h"

// Scripting
#include "luajit/src/lua.hpp"
#include "sol.hpp"
#include "scripting/script_initializer.h"

// Microbe
#include "microbe_stage/compound.h"

// Console
#include "gui/CEGUIWindow.h"

#include "util/contains.h"
#include "util/pair_hash.h"

#include <boost/algorithm/string.hpp>
#include <boost/filesystem.hpp>
#include <boost/lexical_cast.hpp>
#include <chrono>
#include <ctime>
#include <forward_list>
#include <fstream>
#include <iostream>
#include <OgreConfigFile.h>
#include <OgreLogManager.h>
#include <OgreRenderWindow.h>
#include <OgreResourceBackgroundQueue.h>
#include <OgreTextureManager.h>
#include <OgreWindowEventUtilities.h>
#include <OgreRoot.h>
#include <OgreWindowEventUtilities.h>
#include <OISInputManager.h>
#include <OISMouse.h>
#include <OgreTextureManager.h>
#include <map>
#include <random>
#include <set>
#include <stdlib.h>
#include <unordered_map>
#include <iostream>
#include "sound/sound_manager.h"

#include <fstream>

using namespace thrive;

static const char* RESOURCES_CFG = "resources.cfg";
static const char* PLUGINS_CFG   = "plugins.cfg";

////////////////////////////////////////////////////////////////////////////////
// Engine
////////////////////////////////////////////////////////////////////////////////

struct Engine::Implementation : public Ogre::WindowEventListener {

    Implementation(
        Engine& engine
    ) : m_engine(engine),
        m_rng(),
        m_playerData("player")
    {
    }

    ~Implementation() {
        Ogre::WindowEventUtilities::removeWindowEventListener(
            m_graphics.renderWindow,
            this
        );
    }


    void
    loadSavegame() {
        std::ifstream stream(
            m_serialization.loadFile,
            std::ifstream::binary
        );
        m_serialization.loadFile = "";
        stream.clear();
        stream.exceptions(std::ofstream::failbit | std::ofstream::badbit);
        StorageContainer savegame;
        try {
            stream >> savegame;
        }
        catch(const std::ofstream::failure& e) {
            std::cerr << "Error loading file: " << e.what() << std::endl;
            throw;
        }
        
        // Load game states
        sol::protected_function luaMethod = m_luaState["g_luaEngine"]
            ["loadSavegameGameStates"];

        luaMethod.error_handler = m_luaState["thrivePanic"];
    
        if(!luaMethod(m_luaState["g_luaEngine"], &savegame).valid()){

            throw std::runtime_error("LuaEngine failed to load saved game states");
        }
                
        m_playerData.load(savegame.get<StorageContainer>("playerData"));
    }

    void
    loadOgreConfig() {
        if(not (m_graphics.root->restoreConfig() or m_graphics.root->showConfigDialog()))
        {
            exit(EXIT_SUCCESS);
        }
    }

    void
    loadResources() {
        Ogre::ConfigFile config;
        config.load(RESOURCES_CFG);
        auto sectionIter = config.getSectionIterator();
        auto& resourceManager = Ogre::ResourceGroupManager::getSingleton();
        while (sectionIter.hasMoreElements()) {
            std::string sectionName = sectionIter.peekNextKey();
            Ogre::ConfigFile::SettingsMultiMap* sectionContent = sectionIter.getNext();
            for(auto& setting : *sectionContent) {
                std::string resourceType = setting.first;
                std::string resourceLocation = setting.second;
                resourceManager.addResourceLocation(
                    resourceLocation,
                    resourceType,
                    sectionName
                );
            }
        }
    }

    /**
    * @brief Loads lua scripts from folder.
    *
    * Looks for a "manifest.txt" to determine which files to load
    * @returns True on success, false on failure
    */
    bool
    loadScripts(
        const boost::filesystem::path& directory
    ) {
        namespace fs = boost::filesystem;
        fs::path manifestPath = directory / "manifest.txt";
        if (not fs::exists(manifestPath)) {
            throw std::runtime_error("Missing manifest file: " + manifestPath.string());
            return false;
        }
        std::ifstream manifest(manifestPath.string());
        if (not manifest.is_open()) {
            throw std::runtime_error("Could not open manifest file: " + manifestPath.string());
        }
        std::string line;
        while(not manifest.eof()) {
            std::getline(manifest, line);
            boost::algorithm::trim(line);
            if (line.empty() or line.find("//") == 0) {
                continue;
            }
            fs::path manifestEntryPath = directory / line;
            if (not fs::exists(manifestEntryPath)) {
                std::cerr << "Warning: Could not find file " << manifestEntryPath.string() << std::endl;
                continue;
            }
            else if (fs::is_directory(manifestEntryPath)) {
                bool success = this->loadScripts(manifestEntryPath);

                if(!success)
                    return false;
            }
            else {

                sol::protected_function fileFunc =
                    m_luaState.load_file(manifestEntryPath.string().c_str());

                fileFunc.error_handler = m_luaState["thrivePanic"];
                
                auto runResult = fileFunc();

                if(runResult.status() != sol::call_status::ok){

                    std::cerr << "Failed to run Lua file: " << manifestEntryPath.string() <<
                        std::endl << " error: " << runResult.get<std::string>() <<
                        std::endl;
                    
                    return false;
                }
            }
        }

        return true;
    }

    void
    saveSavegame() {
        StorageContainer savegame;

        // Load game states
        sol::protected_function luaMethod = m_luaState["g_luaEngine"]
            ["saveCurrentStates"];

        luaMethod.error_handler = m_luaState["thrivePanic"];
    
        if(!luaMethod(m_luaState["g_luaEngine"], &savegame).valid()){

            throw std::runtime_error("LuaEngine failed to save game states");
        }

        savegame.set("playerData", m_playerData.storage());
        
        savegame.set("thriveversion", m_thriveVersion);
        std::ofstream stream(
            m_serialization.saveFile,
            std::ofstream::trunc | std::ofstream::binary
        );
        m_serialization.saveFile = "";
        stream.exceptions(std::ofstream::failbit | std::ofstream::badbit);
        if (stream) {
            try {
                stream << savegame;
                stream.flush();
                stream.close();
            }
            catch (const std::ofstream::failure& e) {
                std::cerr << "Error saving file: " << e.what() << std::endl;
                throw;
            }
        }
        else {
            std::perror("Could not open file for saving");
        }
    }

    void
    setupGraphics() {
        m_graphics.root.reset(new Ogre::Root(PLUGINS_CFG));
        this->loadResources();
        this->loadOgreConfig();
        m_graphics.renderWindow = m_graphics.root->initialise(true, "Thrive");
        m_input.mouse.setWindowSize(
            m_graphics.renderWindow->getWidth(),
            m_graphics.renderWindow->getHeight()
        );
        Ogre::WindowEventUtilities::addWindowEventListener(
            m_graphics.renderWindow,
            this
        );
        // Set default mipmap level (NB some APIs ignore this)
        Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(5);
        // initialise all resource groups
        Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();

        // Load video player
        VideoPlayer::loadFFMPEG();
    }

    void
    setupInputManager() {
        const std::string HANDLE_NAME = "WINDOW";
        size_t windowHandle = 0;
        m_graphics.renderWindow->getCustomAttribute(HANDLE_NAME, &windowHandle);
        OIS::ParamList parameters;
        parameters.insert(std::make_pair(
            HANDLE_NAME,
            boost::lexical_cast<std::string>(windowHandle)
        ));
#if defined OIS_WIN32_PLATFORM
        parameters.insert(std::make_pair(std::string("w32_mouse"), std::string("DISCL_FOREGROUND" )));
        parameters.insert(std::make_pair(std::string("w32_mouse"), std::string("DISCL_EXCLUSIVE")));
        parameters.insert(std::make_pair(std::string("w32_keyboard"), std::string("DISCL_FOREGROUND")));
        parameters.insert(std::make_pair(std::string("w32_keyboard"), std::string("DISCL_NONEXCLUSIVE")));
#elif defined OIS_LINUX_PLATFORM
        parameters.insert(std::make_pair(std::string("x11_mouse_grab"), std::string("false")));
        parameters.insert(std::make_pair(std::string("x11_mouse_hide"), std::string("true")));
        parameters.insert(std::make_pair(std::string("x11_keyboard_grab"), std::string("false")));
        parameters.insert(std::make_pair(std::string("XAutoRepeatOn"), std::string("true")));
#endif
        m_input.inputManager = OIS::InputManager::createInputSystem(parameters);
        m_input.keyboard.init(m_input.inputManager, m_aggregator.get());
        m_input.mouse.init(m_input.inputManager, m_aggregator.get());
    }

    void
    setupGUI(){

        // Start loading gui Images needed by AlphaHitWindow //
        // TODO: see if we can use the same Texture that CEGUI loads for itself
        Ogre::ResourceBackgroundQueue::getSingleton().load(
            Ogre::Root::getSingleton().getTextureManager()->getResourceType(),
            "ThriveGeneric.png", Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);
        
        CEGUI::WindowFactoryManager::addFactory<CEGUI::TplWindowFactory<AlphaHitWindow> >();

        CEGUI::OgreRenderer::bootstrapSystem();
        CEGUI::WindowManager& wmgr = CEGUI::WindowManager::getSingleton();
        CEGUI::Window* myRoot = wmgr.createWindow( "DefaultWindow", "root" );
        myRoot->setProperty("CursorPassThroughEnabled", "True");

        CEGUI::System::getSingleton().getDefaultGUIContext().setRootWindow( myRoot );
        CEGUI::SchemeManager::getSingleton().createFromFile("Thrive.scheme");
        CEGUI::System::getSingleton().getDefaultGUIContext().getCursor().setDefaultImage(
            "ThriveGeneric/MouseArrow");

        m_aggregator = std::move(std::unique_ptr<CEGUI::InputAggregator>(
                new CEGUI::InputAggregator(&CEGUI::System::getSingleton()
                    .getDefaultGUIContext())));

        // Using the handling on keydown mode to detect when inputs are consumed
        m_aggregator->initialise(false);

        CEGUI::System::getSingleton().getDefaultGUIContext().setDefaultTooltipType("Thrive/Tooltip");

        // For demos
        // This file is renamed in newer CEGUI versions
      //  CEGUI::System::getSingleton().getDefaultGUIContext().getMouseCursor().setDefaultImage(
       //     "ThriveGeneric/MouseArrow");

       // CEGUI::SchemeManager::getSingleton().createFromFile("GameMenu.scheme");

      //  CEGUI::ImageManager::getSingleton().loadImageset("GameMenu.imageset");
       // CEGUI::ImageManager::getSingleton().loadImageset("HUDDemo.imageset");

        CEGUI::System::getSingleton().getDefaultGUIContext().setDefaultTooltipType("Thrive/Tooltip");
        CEGUI::AnimationManager::getSingleton().loadAnimationsFromXML("thrive.anims");

        //For demos:
        CEGUI::SchemeManager::getSingleton().createFromFile("TaharezLook.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("SampleBrowser.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("OgreTray.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("AlfiskoSkin.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("WindowsLook.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("VanillaSkin.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("Generic.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("VanillaCommonDialogs.scheme");

        CEGUI::ImageManager::getSingleton().loadImageset("DriveIcons.imageset");
        CEGUI::ImageManager::getSingleton().loadImageset("HUDDemo.imageset");
    }

    void
    setupLog() {
        static Ogre::LogManager logManager;
        logManager.createLog("ogre.log", true, false, false);
    }

    void
    setupScripts() {
        initializeLua(m_luaState);
    }

    void
    setupSoundManager() {
        static const std::string DEVICE_NAME = "";

        m_soundManager = std::move(std::unique_ptr<SoundManager>(new SoundManager()));

        m_soundManager->init(DEVICE_NAME);
        //soundManager.setDistanceModel(AL_LINEAR_DISTANCE);
    }

    void
    loadVersionNumber() {
        std::ifstream versionFile ("thriveversion.ver");
        if (versionFile.is_open()) {
            std::getline(versionFile, m_thriveVersion);
        }
        else {
            m_thriveVersion = "unknown";
        }
        versionFile.close();
    }

    void
    shutdownInputManager() {
        if (not m_input.inputManager) {
            return;
        }
        OIS::InputManager::destroyInputSystem(m_input.inputManager);
        m_input.inputManager = nullptr;
    }

    bool
    windowClosing(
        Ogre::RenderWindow* window
    ) override {
        if (window == m_graphics.renderWindow) {
            m_quitRequested = true;
        }
        return true;
    }

    void
    windowResized(
        Ogre::RenderWindow* window
    ) override {
        if (window == m_graphics.renderWindow) {
            m_input.mouse.setWindowSize(
                window->getWidth(),
                window->getHeight()
            );
        }
        CEGUI::System::getSingleton().getRenderer()->setDisplaySize(CEGUI::Sizef(window->getWidth(), window->getHeight()));
    }
    // Lua state must be one of the last to be destroyed, so keep it
    // at top. The reason for that is that some components keep
    // sol::object instances around that rely on the lua state to
    // still exist when they are destroyed. Since those components are
    // destroyed with the entity manager, the lua state has to live
    // longer than the manager.
    sol::state m_luaState;

    ComponentFactory m_componentFactory;

    Engine& m_engine;

    RNG m_rng;

    PlayerData m_playerData;

    bool m_quitRequested = false;

    bool m_paused = false;

    struct Graphics {

        std::unique_ptr<Ogre::Root> root;

        Ogre::RenderWindow* renderWindow = nullptr;

    } m_graphics;

    struct Input {

        OIS::InputManager* inputManager = nullptr;

        Keyboard keyboard;

        Mouse mouse;

    } m_input;

    std::string m_thriveVersion;

    struct Serialization {

        std::string loadFile;

        std::string saveFile;

    } m_serialization;

    std::unique_ptr<SoundManager> m_soundManager;
    std::unique_ptr<CEGUI::InputAggregator> m_aggregator;
};

void Engine::luaBindings(
    sol::state &lua
){    
    lua.new_usertype<Engine>("__Engine",

        "new", sol::no_constructor,

        "playerData", &Engine::playerData,
        "load", &Engine::load,
        "save", &Engine::save,
        "fileExists", &Engine::fileExists,
        "saveCreation", static_cast<void(Engine::*)(EntityId, std::string,
            std::string)const>(&Engine::saveCreation),
        "loadCreation", static_cast<EntityId(Engine::*)(std::string)>(&Engine::loadCreation),
        "screenShot", &Engine::screenShot,
        "getCreationFileList", &Engine::getCreationFileList,
        "quit", &Engine::quit,
        "thriveVersion", &Engine::thriveVersion,
        "pauseGame", &Engine::pauseGame,
        "resumeGame", &Engine::resumeGame,
        "getResolutionHeight", &Engine::getResolutionHeight,
        "getResolutionWidth", &Engine::getResolutionWidth,
        "componentFactory", sol::property(&Engine::componentFactory),
        "keyboard", sol::property(&Engine::keyboard),
        "mouse", sol::property(&Engine::mouse)
    );
}

void
Engine::pauseGame(){
 m_impl->m_paused = true;
}

void
Engine::resumeGame(){
 m_impl->m_paused = false;
}

Engine::Engine()
  : m_impl(new Implementation(*this))
{
}


Engine::~Engine() { }


ComponentFactory&
Engine::componentFactory() {
    return m_impl->m_componentFactory;
}

RNG&
Engine::rng() {
    return m_impl->m_rng;
}

void
Engine::init() {
    assert(m_impl->m_currentGameState == nullptr);
    std::srand(unsigned(time(0)));
    m_impl->setupLog();
    m_impl->setupScripts();
    m_impl->loadVersionNumber();
    
    m_impl->setupGraphics();
    m_impl->setupGUI();
    
    m_impl->setupInputManager();
    
    if(!m_impl->loadScripts("../scripts")){

        throw std::runtime_error("Engine failed to load Lua scripts");
    }


    // Initialize lua engine side
    sol::protected_function luaInit = m_impl->m_luaState["g_luaEngine"]["init"];

    luaInit.error_handler = m_impl->m_luaState["thrivePanic"];
    
    if(!luaInit(m_impl->m_luaState["g_luaEngine"], this).valid()){

        throw std::runtime_error("Failed to initialize LuaEngine side");
    }
    
    // OgreOggSoundManager must be initialized after at least one
    // Ogre::SceneManager has been instantiated so we need to hope
    // that the lua engine had a gamestate to initialize that uses
    // Ogre
    m_impl->setupSoundManager();

}

void
Engine::enterLuaMain(
    Game* gameObj
) {
    sol::protected_function luaMain = m_impl->m_luaState["enterLuaMain"];

    luaMain.error_handler = m_impl->m_luaState["thrivePanic"];

    luaMain(gameObj);
}

EntityId
Engine::transferEntityGameState(
    EntityId id,
    EntityManager* entityManager,
    GameStateData* targetState
) {
    sol::protected_function luaMethod = m_impl->m_luaState["g_luaEngine"]
        ["transferEntityGameState"];

    luaMethod.error_handler = m_impl->m_luaState["thrivePanic"];

    auto result = luaMethod(m_impl->m_luaState["g_luaEngine"],
        id, entityManager, targetState);
    
    if(!result.valid())
    {
        throw std::runtime_error("Failed call LuaEngine:transferEntityGameState");
    }

    return result.get<EntityId>();
}

bool
Engine::isSystemTimedShutdown(
    System* system
) {
    sol::protected_function luaMethod = m_impl->m_luaState["g_luaEngine"]
        ["isSystemTimedShutdown"];

    luaMethod.error_handler = m_impl->m_luaState["thrivePanic"];

    auto result = luaMethod(m_impl->m_luaState["g_luaEngine"],
        system);
    
    if(!result.valid()){
        
        throw std::runtime_error("Failed call LuaEngine:isSystemTimedShutdown");
    }

    return result.get<bool>();
}

void
Engine::timedSystemShutdown(
    System* system,
    int timeInMS
) {
    sol::protected_function luaMethod = m_impl->m_luaState["g_luaEngine"]
        ["timedSystemShutdown"];

    luaMethod.error_handler = m_impl->m_luaState["thrivePanic"];

    auto result = luaMethod(m_impl->m_luaState["g_luaEngine"],
        system, timeInMS);
    
    if(!result.valid()){
        
        throw std::runtime_error("Failed call LuaEngine:timedSystemShutdown");
    }
}

GameStateData*
Engine::getCurrentGameStateFromLua(
) {

    sol::optional<GameStateData*> state = m_impl->m_luaState["g_luaEngine"]["currentGameState"]
        ["wrapper"];

    if(!state)
        throw std::runtime_error("Engine: getCurrentGameStateFromLua failed to "
            "get value (is state null?)");

    GameStateData* statePtr = state.value();

    if(!statePtr)
        throw std::runtime_error("Engine: current GameStateData is nullptr");
    
    return statePtr;
}


OIS::InputManager*
Engine::inputManager() const {
    return m_impl->m_input.inputManager;
}


const Keyboard&
Engine::keyboard() const {
    return m_impl->m_input.keyboard;
}


void
Engine::load(
    std::string filename
) {
    m_impl->m_serialization.loadFile = filename;
}


bool
Engine::fileExists(
    std::string filePath
) {
        namespace fs = boost::filesystem;
        fs::path fPath = filePath;
        if (not fs::exists(fPath)) {
            return false;
        }
        else{
            return true;
        }

}


lua_State*
Engine::luaState(){
    return m_impl->m_luaState;
}

const Mouse&
Engine::mouse() const {
    return m_impl->m_input.mouse;
}


Ogre::Root*
Engine::ogreRoot() const {
    return m_impl->m_graphics.root.get();
}


Ogre::RenderWindow*
Engine::renderWindow() const {
    return m_impl->m_graphics.renderWindow;
}


void
Engine::save(
    std::string filename
) {
    m_impl->m_serialization.saveFile = filename;
}

void
Engine::saveCreation(
    EntityId entityId,
    std::string name,
    std::string type
) const {

    // Get current EntityManager
    EntityManager* currentManager = m_impl->m_luaState["g_luaEngine"]["currentGameState"]
        ["entityManager"];

    if(currentManager == nullptr)
        throw std::runtime_error("saveCreation got nullptr as current EntityManager");

    saveCreation(entityId, *currentManager, name, type);
}

void
Engine::saveCreation(
    EntityId entityId,
    const EntityManager& entityManager,
    std::string name,
    std::string type
) const {
    namespace fs = boost::filesystem;
    StorageContainer creation = entityManager.storeEntity(entityId);
    fs::path pth = (fs::path("creations") / fs::path(type));
    boost::system::error_code returnedError;
    boost::filesystem::create_directories( pth, returnedError );
    if (returnedError) {
        std::perror("Could not create necessary directories for saving.");
    }
    else {
        creation.set("thriveversion", this->thriveVersion());
        std::ofstream stream(
            (pth / fs::path(name + "." + type)).string<std::string>(),
            std::ofstream::trunc | std::ofstream::binary
        );
        stream.exceptions(std::ofstream::failbit | std::ofstream::badbit);
        if (stream) {
            try {
                stream << creation;
                stream.flush();
                stream.close();
            }
            catch (const std::ofstream::failure& e) {
                std::cerr << "Error saving file: " << e.what() << std::endl;
                throw;
            }
        }
        else {
            std::perror("Could not open file for saving");
        }
    }
}

EntityId
Engine::loadCreation(
    std::string file
) {
    // Get current EntityManager
    EntityManager* currentManager = m_impl->m_luaState["g_luaEngine"]["currentGameState"]
        ["entityManager"];

    if(currentManager == nullptr)
        throw std::runtime_error("loadCreation got nullptr as current EntityManager");
    
    return loadCreation(file, *currentManager);
}

EntityId
Engine::loadCreation(
    std::string file,
    EntityManager& entityManager
) {
    std::ifstream stream(
        file,
        std::ifstream::binary
    );
    stream.clear();
    stream.exceptions(std::ofstream::failbit | std::ofstream::badbit);
    StorageContainer creation;
    try {
        stream >> creation;
    }
    catch(const std::ofstream::failure& e) {
        std::cerr << "Error loading file: " << e.what() << std::endl;
        throw;
    }
    EntityId entityId = entityManager.loadEntity(creation, m_impl->m_componentFactory);
    return entityId;
}

void
Engine::screenShot(std::string path){
     m_impl->m_graphics.renderWindow->writeContentsToFile(path);
}



std::string
Engine::getCreationFileList(
    std::string stage
) const {
    namespace fs = boost::filesystem;
    fs::path directory("./creations/" + stage);
    fs::directory_iterator end_iter;
    std::stringstream stringbuilder;
    if ( fs::exists(directory) && fs::is_directory(directory)) {
        for( fs::directory_iterator dir_iter(directory) ; dir_iter != end_iter ; ++dir_iter) {
            if (fs::is_regular_file(dir_iter->status()) )
            {
                stringbuilder << dir_iter->path().string() << " ";
            }
        }
    }
    return stringbuilder.str();
}

PlayerData&
Engine::playerData(){
    return m_impl->m_playerData;
}

void
Engine::shutdown() {
    
    sol::protected_function luaInit = m_impl->m_luaState["g_luaEngine"]["shutdown"];

    luaInit.error_handler = m_impl->m_luaState["thrivePanic"];
    
    if(!luaInit(m_impl->m_luaState["g_luaEngine"]).valid()){

        throw std::runtime_error("Failed to shutdown LuaEngine side");
    }
    
    m_impl->shutdownInputManager();
    m_impl->m_graphics.renderWindow->destroy();

    m_impl->m_graphics.root.reset();
}


void
Engine::quit(){
    m_impl->m_quitRequested = true;
}

SoundManager*
Engine::soundManager() const {
    return m_impl->m_soundManager.get();
}

void
Engine::update(
    int milliseconds
) {
    if (not m_impl->m_serialization.saveFile.empty()) {
        m_impl->saveSavegame();
    }
    Ogre::WindowEventUtilities::messagePump();
    if (m_impl->m_quitRequested) {
        Game::instance().quit();
        return;
    }
    m_impl->m_input.keyboard.update();
    m_impl->m_input.mouse.update();

    CEGUI::System::getSingleton().injectTimePulse(milliseconds/1000.0f);
    CEGUI::System::getSingleton().getDefaultGUIContext().injectTimePulse(milliseconds/1000.0f);

    if (not m_impl->m_serialization.loadFile.empty()) {
        m_impl->loadSavegame();
    }
}

int
Engine::getResolutionWidth() const {
    return m_impl->m_graphics.renderWindow->getWidth();
}

int
Engine::getResolutionHeight() const {
    return m_impl->m_graphics.renderWindow->getHeight();
}

const std::string&
Engine::thriveVersion() const {
    return m_impl->m_thriveVersion;
}

