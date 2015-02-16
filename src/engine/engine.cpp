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
#include "ogre/text_overlay.h"

// Scripting
#include <luabind/iterator_policy.hpp>
#include "scripting/luabind.h"
#include "scripting/lua_state.h"
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
#include <luabind/adopt_policy.hpp>
#include <OgreConfigFile.h>
#include <OgreLogManager.h>
#include <OgreOggSoundManager.h>
#include <OgreRenderWindow.h>
#include <OgreRoot.h>
#include <OgreWindowEventUtilities.h>
#include <OISInputManager.h>
#include <OISMouse.h>
#include <map>
#include <random>
#include <set>
#include <stdlib.h>
#include <unordered_map>

#include <iostream>

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
        m_playerData("player"),
        m_nextShutdownSystems(new std::map<System*, int>),
        m_prevShutdownSystems(new std::map<System*, int>)
    {
    }

    ~Implementation() {
        Ogre::WindowEventUtilities::removeWindowEventListener(
            m_graphics.renderWindow,
            this
        );
    }

    void
    activateGameState(
        GameState* gameState
    ) {
        if (m_currentGameState) {
            m_currentGameState->deactivate();
        }
        m_currentGameState = gameState;
        if (gameState) {
            gameState->activate();
            gameState->rootGUIWindow().addChild(m_consoleGUIWindow);
            luabind::call_member<void>(m_console, "registerEvents", gameState);
        }
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
        GameState* previousGameState = m_currentGameState;
        this->activateGameState(nullptr);
        StorageContainer gameStates = savegame.get<StorageContainer>("gameStates");
        for (const auto& pair : m_gameStates) {
            if (gameStates.contains(pair.first)) {
                // In case anything relies on the current game state
                // during loading, temporarily switch it
                m_currentGameState = pair.second.get();
                pair.second->load(
                    gameStates.get<StorageContainer>(pair.first)
                );
            }
            else {
                pair.second->entityManager().clear();
            }
        }
        for (auto& kv : *m_prevShutdownSystems) {
            kv.first->deactivate();
        }
        for (auto& kv : *m_nextShutdownSystems) {
            kv.first->deactivate();
        }
        m_prevShutdownSystems->clear();
        m_nextShutdownSystems->clear();
        m_currentGameState = nullptr;
        // Switch gamestate
        std::string gameStateName = savegame.get<std::string>("currentGameState");
        auto iter = m_gameStates.find(gameStateName);
        if (iter != m_gameStates.end()) {
            this->activateGameState(iter->second.get());
        }
        else {
            this->activateGameState(previousGameState);
            // TODO: Log error
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

    void
    loadScripts(
        const boost::filesystem::path& directory
    ) {
        namespace fs = boost::filesystem;
        fs::path manifestPath = directory / "manifest.txt";
        if (not fs::exists(manifestPath)) {
            return;
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
                this->loadScripts(manifestEntryPath);
            }
            else {
                int error = 0;
                error = luaL_loadfile(
                    m_luaState,
                    manifestEntryPath.string().c_str()
                );
                error = error or luabind::detail::pcall(m_luaState, 0, LUA_MULTRET);
                if (error) {
                    std::string errorMessage = lua_tostring(m_luaState, -1);
                    lua_pop(m_luaState, 1);
                    std::cerr << errorMessage << std::endl;
                }
            }
        }
    }

    void
    saveSavegame() {
        StorageContainer savegame;
        savegame.set("currentGameState", m_currentGameState->name());
        savegame.set("playerData", m_playerData.storage());
        StorageContainer gameStates;
        for (const auto& pair : m_gameStates) {
            gameStates.set(pair.first, pair.second->storage());
        }
        savegame.set("gameStates", std::move(gameStates));
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
        m_input.keyboard.init(m_input.inputManager);
        m_input.mouse.init(m_input.inputManager);
    }

    void
    setupGUI(){
        CEGUI::WindowFactoryManager::addFactory<CEGUI::TplWindowFactory<AlphaHitWindow> >();

        CEGUI::OgreRenderer::bootstrapSystem();
        CEGUI::WindowManager& wmgr = CEGUI::WindowManager::getSingleton();
        CEGUI::Window* myRoot = wmgr.createWindow( "DefaultWindow", "root" );
        myRoot->setProperty("MousePassThroughEnabled", "True");
        CEGUI::System::getSingleton().getDefaultGUIContext().setRootWindow( myRoot );
        CEGUI::SchemeManager::getSingleton().createFromFile("Thrive.scheme");
        CEGUI::System::getSingleton().getDefaultGUIContext().getMouseCursor().setDefaultImage("ThriveGeneric/MouseArrow");

        CEGUI::AnimationManager::getSingleton().loadAnimationsFromXML("thrive.anims");

        //For demos:
        CEGUI::SchemeManager::getSingleton().createFromFile("TaharezLook.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("SampleBrowser.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("OgreTray.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("GameMenu.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("AlfiskoSkin.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("WindowsLook.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("VanillaSkin.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("Generic.scheme");
        CEGUI::SchemeManager::getSingleton().createFromFile("VanillaCommonDialogs.scheme");

        CEGUI::ImageManager::getSingleton().loadImageset("DriveIcons.imageset");
        CEGUI::ImageManager::getSingleton().loadImageset("GameMenu.imageset");
        CEGUI::ImageManager::getSingleton().loadImageset("HUDDemo.imageset");

        m_consoleGUIWindow = new CEGUIWindow("Console");
    }

    void
    setupLog() {
        static Ogre::LogManager logManager;
        logManager.createLog("default", true, false, false);
    }

    void
    setupScripts() {
        initializeLua(m_luaState);
    }

    void
    setupSoundManager() {
        static const std::string DEVICE_NAME = "";
        static const unsigned int MAX_SOURCES = 100;
        static const unsigned int QUEUE_LIST_SIZE = 100;
        auto& soundManager = OgreOggSound::OgreOggSoundManager::getSingleton();
        soundManager.init(
            DEVICE_NAME,
            MAX_SOURCES,
            QUEUE_LIST_SIZE
        );
        soundManager.setDistanceModel(AL_LINEAR_DISTANCE);
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

    // Lua state must be one of the last to be destroyed, so keep it at top.
    // The reason for that is that some components keep luabind::object
    // instances around that rely on the lua state to still exist when they
    // are destroyed. Since those components are destroyed with the entity
    // manager, the lua state has to live longer than the manager.
    LuaState m_luaState;

    GameState* m_currentGameState = nullptr;

    CEGUIWindow* m_consoleGUIWindow = nullptr;

    ComponentFactory m_componentFactory;

    Engine& m_engine;

    std::map<std::string, std::unique_ptr<GameState>> m_gameStates;

    std::list<std::tuple<EntityId, EntityId, GameState*, GameState*>> m_entitiesToTransferGameState;

    RNG m_rng;

    PlayerData m_playerData;

    bool m_quitRequested = false;

    bool m_paused = false;

    std::map<System*, int>* m_nextShutdownSystems;
    std::map<System*, int>* m_prevShutdownSystems;

    struct Graphics {

        std::unique_ptr<Ogre::Root> root;

        Ogre::RenderWindow* renderWindow = nullptr;

    } m_graphics;

    struct Input {

        OIS::InputManager* inputManager = nullptr;

        Keyboard keyboard;

        Mouse mouse;

    } m_input;

    GameState* m_nextGameState = nullptr;

    std::string m_thriveVersion;

    struct Serialization {

        std::string loadFile;

        std::string saveFile;

    } m_serialization;

    luabind::object m_console;
};


static GameState*
Engine_createGameState(
    Engine* self,
    std::string name,
    luabind::object luaSystems,
    luabind::object luaInitializer,
    std::string guiLayoutName
) {
    std::vector<std::unique_ptr<System>> systems;
    for (luabind::iterator iter(luaSystems), end; iter != end; ++iter) {
        System* system = luabind::object_cast<System*>(
            *iter,
            luabind::adopt(luabind::result)
        );
        systems.emplace_back(system);
    }
    // We can't just capture the luaInitializer in the lambda here, because
    // luabind::object's call operator is not const
    auto initializer = std::bind<void>(
        [](luabind::object luaInitializer) {
            luaInitializer();
        },
        luaInitializer
    );
    return self->createGameState(
        name,
        std::move(systems),
        initializer,
        guiLayoutName
    );
}


luabind::scope
Engine::luaBindings() {
    using namespace luabind;
    return class_<Engine>("__Engine")
        .def("createGameState", Engine_createGameState)
        .def("currentGameState", &Engine::currentGameState)
        .def("getGameState", &Engine::getGameState)
        .def("setCurrentGameState", &Engine::setCurrentGameState)
        .def("playerData", &Engine::playerData)
        .def("load", &Engine::load)
        .def("save", &Engine::save)
        .def("fileExists", &Engine::fileExists)
        .def("saveCreation", static_cast<void(Engine::*)(EntityId, std::string, std::string)const>(&Engine::saveCreation))
        .def("loadCreation", static_cast<EntityId(Engine::*)(std::string)>(&Engine::loadCreation))
        .def("getCreationFileList", &Engine::getCreationFileList)
        .def("quit", &Engine::quit)
        .def("timedSystemShutdown", &Engine::timedSystemShutdown)
        .def("isSystemTimedShutdown", &Engine::isSystemTimedShutdown)
        .def("thriveVersion", &Engine::thriveVersion)
        .def("pauseGame", &Engine::pauseGame)
        .def("resumeGame", &Engine::resumeGame)
        .def("registerConsoleObject", &Engine::registerConsoleObject)
        .property("componentFactory", &Engine::componentFactory)
        .property("keyboard", &Engine::keyboard)
        .property("mouse", &Engine::mouse)
    ;
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


GameState*
Engine::createGameState(
    std::string name,
    std::vector<std::unique_ptr<System>> systems,
    GameState::Initializer initializer,
    std::string guiLayoutName
) {
    assert(m_impl->m_gameStates.find(name) == m_impl->m_gameStates.end() && "Duplicate GameState name");
    std::unique_ptr<GameState> gameState(new GameState(
        *this,
        name,
        std::move(systems),
        initializer,
        guiLayoutName
    ));
    GameState* rawGameState = gameState.get();
    m_impl->m_gameStates.insert(std::make_pair(
        name,
        std::move(gameState)
    ));
    return rawGameState;
}


GameState*
Engine::currentGameState() const {
    return m_impl->m_currentGameState;
}

RNG&
Engine::rng() {
    return m_impl->m_rng;
}


GameState*
Engine::getGameState(
    const std::string& name
) const {
    auto iter = m_impl->m_gameStates.find(name);
    if (iter != m_impl->m_gameStates.end()) {
        return iter->second.get();
    }
    else {
        return nullptr;
    }
}


void
Engine::init() {
    assert(m_impl->m_currentGameState == nullptr);
    std::srand(unsigned(time(0)));
    m_impl->setupLog();
    m_impl->setupScripts();
    m_impl->setupGraphics();
    m_impl->setupInputManager();
    m_impl->setupGUI();
    m_impl->loadScripts("../scripts");
    m_impl->loadVersionNumber();
    GameState* previousGameState = m_impl->m_currentGameState;
    for (const auto& pair : m_impl->m_gameStates) {
        const auto& gameState = pair.second;
        m_impl->m_currentGameState = gameState.get();
        gameState->init();
    }
    // OgreOggSoundManager must be initialized after at least one
    // Ogre::SceneManager has been instantiated
    m_impl->setupSoundManager();
    m_impl->m_currentGameState = previousGameState;
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
    saveCreation(entityId, this->currentGameState()->entityManager(), name, type);
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
    return loadCreation(file, this->currentGameState()->entityManager());
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

void
Engine::setCurrentGameState(
    GameState* gameState
) {
    assert(gameState != nullptr && "GameState must not be null");
    m_impl->m_nextGameState = gameState;
    for (auto& pair : *m_impl->m_prevShutdownSystems){
        //Make sure systems are deactivated before any potential reactivations
        pair.first->deactivate();
    }
    m_impl->m_prevShutdownSystems = m_impl->m_nextShutdownSystems;
    m_impl->m_nextShutdownSystems = m_impl->m_prevShutdownSystems;
    m_impl->m_nextShutdownSystems->clear();
}

PlayerData&
Engine::playerData(){
    return m_impl->m_playerData;
}

void
Engine::shutdown() {
    for (const auto& pair : m_impl->m_gameStates) {
        const auto& gameState = pair.second;
        gameState->shutdown();
    }
    m_impl->shutdownInputManager();
    m_impl->m_graphics.renderWindow->destroy();
    m_impl->m_graphics.root.reset();
}


void
Engine::quit(){
    m_impl->m_quitRequested = true;
}


OgreOggSound::OgreOggSoundManager*
Engine::soundManager() const {
    return OgreOggSound::OgreOggSoundManager::getSingletonPtr();
}

EntityId
Engine::transferEntityGameState(
    EntityId oldEntityId,
    EntityManager* oldEntityManager,
    GameState* newGameState
){
    EntityId newEntity;
    const std::string* nameMapping = oldEntityManager->getNameMappingFor(oldEntityId);
    if (nameMapping){
        newEntity = newGameState->entityManager().getNamedId(*nameMapping, true);
    }
    else{
        newEntity = newGameState->entityManager().generateNewId();
    }
    oldEntityManager->transferEntity(oldEntityId, newEntity, newGameState->entityManager(), m_impl->m_componentFactory);
    return newEntity;
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

    if (m_impl->m_nextGameState) {
        m_impl->activateGameState(m_impl->m_nextGameState);
        m_impl->m_nextGameState = nullptr;
    }
    assert(m_impl->m_currentGameState != nullptr);
    m_impl->m_currentGameState->update(milliseconds, m_impl->m_paused ? 0 : milliseconds);

    luabind::call_member<void>(m_impl->m_console, "update");

    CEGUI::System::getSingleton().injectTimePulse(milliseconds/1000.0f);

    // Update any timed shutdown systems
    auto itr = m_impl->m_prevShutdownSystems->begin();
    while (itr != m_impl->m_prevShutdownSystems->end()) {
        int updateTime = std::min(itr->second, milliseconds);
        itr->first->update(updateTime, m_impl->m_paused ? 0 : updateTime);
        itr->second = itr->second - updateTime;
        if (itr->second == 0) {
            // Remove systems that had timed out
            itr->first->deactivate();
            m_impl->m_prevShutdownSystems->erase(itr++);
        } else {
            ++itr;
        }
    }
    if (not m_impl->m_serialization.loadFile.empty()) {
        m_impl->loadSavegame();
    }
}

void
Engine::timedSystemShutdown(
    System& system,
    int milliseconds
) {
    (*m_impl->m_nextShutdownSystems)[&system] = milliseconds;
}

bool
Engine::isSystemTimedShutdown(
    System& system
) const {
    return m_impl->m_prevShutdownSystems->find(&system) !=  m_impl->m_prevShutdownSystems->end();
}

const std::string&
Engine::thriveVersion() const {
    return m_impl->m_thriveVersion;
}

void
Engine::registerConsoleObject(luabind::object consoleObject) {
    m_impl->m_console = consoleObject;
}
