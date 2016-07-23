#include "sound_source_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/rng.h"
#include "engine/serialization.h"
#include "sound/sound_emitter.h"
#include "sound/sound_manager.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include "game.h"

#include <OgreSceneNode.h>


using namespace thrive;

static const int FADE_TIME = 5000; //5 seconds


////////////////////////////////////////////////////////////////////////////////
// Sound
////////////////////////////////////////////////////////////////////////////////

luabind::scope
Sound::luaBindings() {
    using namespace luabind;
    return class_<Sound>("Sound")
        .scope [
            class_<Properties, Touchable>("Properties")
                .def_readwrite("playState", &Properties::playState)
                .def_readwrite("loop", &Properties::loop)
                .def_readwrite("volume", &Properties::volume)
                .def_readwrite("maxDistance", &Properties::maxDistance)
                .def_readwrite("rolloffFactor", &Properties::rolloffFactor)
                .def_readwrite("referenceDistance", &Properties::referenceDistance)
                .def_readwrite("priority", &Properties::priority)
        ]
        .enum_("PlayState") [
            value("Play", PlayState::Play),
            value("Pause", PlayState::Pause),
            value("Stop", PlayState::Stop)
        ]
        .def(constructor<std::string, std::string>())
        .def("name", &Sound::name)
        .def("pause", &Sound::pause)
        .def("play", &Sound::play)
        .def("stop", &Sound::stop)
        .def_readonly("properties", &Sound::m_properties)
    ;
}


Sound::Sound()
  : Sound("", "")
{
}


Sound::Sound(
    std::string name,
    std::string filename
) : m_filename(filename),
    m_name(name)
{
}


std::string
Sound::filename() const {
    return m_filename;
}


void
Sound::load(
    const StorageContainer& storage
) {
    m_filename = storage.get<std::string>("filename");
    m_name = storage.get<std::string>("name");
    m_properties.playState = static_cast<PlayState>(
        storage.get<int16_t>("playState", PlayState::Stop)
    );
    m_properties.loop = storage.get<bool>("loop");
    m_properties.volume = storage.get<float>("volume");
    m_properties.maxDistance = storage.get<float>("maxDistance", 25.0f);
    m_properties.rolloffFactor = storage.get<float>("rolloffFactor", 0.4f);
    m_properties.referenceDistance = storage.get<float>("referenceDistance", 5.0f);
    m_properties.priority = storage.get<uint8_t>("priority");
}


std::string
Sound::name() const {
    return m_name;
}


void
Sound::play() {
    m_properties.playState = PlayState::Play;
    m_properties.touch();
}


void
Sound::pause() {
    m_properties.playState = PlayState::Pause;
    m_properties.touch();
}


void
Sound::stop() {
    m_properties.playState = PlayState::Stop;
    m_properties.touch();
}


StorageContainer
Sound::storage() const {
    StorageContainer storage;
    storage.set("filename", m_filename);
    storage.set("name", m_name);
    storage.set<int16_t>("playState", m_properties.playState);
    storage.set("loop", m_properties.loop);
    storage.set("volume", m_properties.volume);
    storage.set("maxDistance", m_properties.maxDistance);
    storage.set("rolloffFactor", m_properties.rolloffFactor);
    storage.set("referenceDistance", m_properties.referenceDistance);
    storage.set("priority", m_properties.priority);
    return storage;
}


////////////////////////////////////////////////////////////////////////////////
// SoundSourceComponent
////////////////////////////////////////////////////////////////////////////////

//Luabind helper functions
static bool
SoundSourceComponent_getAmbientSoundSource(
    const SoundSourceComponent* self
) {
    return self->m_ambientSoundSource;
}

static void
SoundSourceComponent_setAmbientSoundSource(
    SoundSourceComponent* self,
    bool value
) {
    self->m_ambientSoundSource = value;
}

static bool
SoundSourceComponent_getAutoLoop(
    const SoundSourceComponent* self
) {
    return self->m_autoLoop;
}

static void
SoundSourceComponent_setAutoLoop(
    SoundSourceComponent* self,
    bool value
) {
    self->m_autoLoop = value;
}

static float
SoundSourceComponent_getVolumeMultiplier(
    const SoundSourceComponent* self
) {
    return self->m_volumeMultiplier;
}

static void
SoundSourceComponent_setVolumeMultiplier(
    SoundSourceComponent* self,
    float value
) {
    self->m_volumeMultiplier = value;
}

luabind::scope
SoundSourceComponent::luaBindings() {
    using namespace luabind;
    return class_<SoundSourceComponent, Component>("SoundSourceComponent")
        .enum_("ID") [
            value("TYPE_ID", SoundSourceComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &SoundSourceComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("addSound", &SoundSourceComponent::addSound)
        .def("removeSound", &SoundSourceComponent::removeSound)
        .def("playSound", &SoundSourceComponent::playSound)
        .def("stopSound", &SoundSourceComponent::stopSound)
        .def("queueSound", &SoundSourceComponent::queueSound)
        .def("interpose", &SoundSourceComponent::interpose)
        .def("interruptPlaying", &SoundSourceComponent::interruptPlaying)
        .property("ambientSoundSource", SoundSourceComponent_getAmbientSoundSource, SoundSourceComponent_setAmbientSoundSource)
        .property("autoLoop", SoundSourceComponent_getAutoLoop, SoundSourceComponent_setAutoLoop)
        .property("volumeMultiplier", SoundSourceComponent_getVolumeMultiplier, SoundSourceComponent_setVolumeMultiplier)
    ;
}

Sound*
SoundSourceComponent::addSound(
    std::string name,
    std::string filename
) {
    auto sound = make_unique<Sound>(name, filename);
    Sound* rawSound = sound.get();
    m_sounds.emplace(name, std::move(sound));
    m_addedSounds.push_back(rawSound);
    return rawSound;
}



void
SoundSourceComponent::removeSound(
    std::string name
) {
    auto iterator = m_sounds.find(name);
    m_removedSounds.push_back(iterator->second.get());
    m_sounds.erase(iterator);
}

void
SoundSourceComponent::playSound(
    std::string name
){
    m_sounds.at(name).get()->play();
}

void
SoundSourceComponent::stopSound(
    std::string name
){
    m_sounds.at(name).get()->stop();
}

void
SoundSourceComponent::interpose(
    std::string name,
    int fadeTime
){
    queueSound(name);
    m_autoSoundCountdown = fadeTime;
}


void
SoundSourceComponent::queueSound(
    std::string name
){
    m_queuedSound = m_sounds.at(name).get();
}

void
SoundSourceComponent::interruptPlaying(){
    m_shouldInteruptPlaying = true;
}

void
SoundSourceComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_ambientSoundSource = storage.get<bool>("ambientSoundSource");
    m_autoLoop = storage.get<bool>("autoLoop");
    StorageList sounds = storage.get<StorageList>("sounds");
    for (const StorageContainer& soundStorage : sounds) {
        auto sound = make_unique<Sound>();
        sound->load(soundStorage);
        m_sounds.emplace(
            sound->name(),
            std::move(sound)
        );
    }
}



StorageContainer
SoundSourceComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set("ambientSoundSource", m_ambientSoundSource.get());
    storage.set("autoLoop", m_autoLoop.get());
    StorageList sounds;
    sounds.reserve(m_sounds.size());
    for (const auto& pair : m_sounds) {
        sounds.push_back(pair.second->storage());
    }
    storage.set<StorageList>("sounds", sounds);
    return storage;
}

REGISTER_COMPONENT(SoundSourceComponent)


////////////////////////////////////////////////////////////////////////////////
// SoundSourceSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
SoundSourceSystem::luaBindings() {
    using namespace luabind;
    return class_<SoundSourceSystem, System>("SoundSourceSystem")
        .def(constructor<>())
    ;
}


struct SoundSourceSystem::Implementation {

    //Destroys all sounds, freeing up memory
    void
    removeAllSounds() {
        for (const auto& item : m_entities) {
            EntityId entityId = item.first;
            this->removeSoundsForEntity(entityId);
        }
        m_sounds.clear();
    }

    //Destroys all sounds for a specific entity (useful for when it is destroyed)
    void
    removeSoundsForEntity(
        EntityId entityId
    ) {
        EntityManager& entityManager = *m_entities.entityManager();
        SoundSourceComponent* soundSource = static_cast<SoundSourceComponent*>(entityManager.getComponent(entityId, SoundSourceComponent::TYPE_ID));
        if (soundSource) {
            for (const auto& pair : soundSource->m_sounds) {
                Sound* sound = pair.second.get();
                sound->m_sound = nullptr;
            }
        }
        for (const auto& pair : m_sounds[entityId]) {
            auto sound = pair.second;
            this->removeSound(sound);
        }
        m_sounds[entityId].clear();
    }

    void
    removeSound(
        SoundEmitter* sound
    ) {
        auto manager = SoundManager::getSingleton();
        if (sound) {
            sound->detachFromNode();
            manager->destroySound(sound);
        }
    }

    //Loads all sounds for all SoundSourceComponenet containing entities
    void
    restoreAllSounds() {
        for (const auto& item : m_entities) {
            EntityId entityId = item.first;
            SoundSourceComponent* soundSourceComponent = std::get<0>(item.second);
            OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(item.second);
            for (const auto& pair : soundSourceComponent->m_sounds) {
                Sound* sound = pair.second.get();
                this->restoreSound(
                    entityId,
                    sceneNodeComponent,
                    sound,
                    soundSourceComponent->m_ambientSoundSource,
                    soundSourceComponent->m_autoLoop,
                    m_gameState
                );
            }
        }
    }

    //Loads a sound and sets relevant properties
    void
    restoreSound(
        EntityId entityId,
        OgreSceneNodeComponent* sceneNodeComponent,
        Sound* sound,
        bool ambient,
        bool autoLoop,
        GameState* gameState
    ) {
        static const bool STREAM = true; //Streaming sound from file

        //3D sounds should not be attempted loaded before scenenodes are created
        if (not ambient && (not sceneNodeComponent || not sceneNodeComponent->m_sceneNode)){
            return;
        }
        std::ostringstream soundName;
        soundName << Game::instance().engine().currentGameState()->name() << sound->name() << entityId;
        auto soundManager = SoundManager::getSingleton();
        auto ogreSound = soundManager->createSound(
            soundName.str(),
            sound->filename(),
            STREAM,
            sound->m_properties.loop,
            gameState->name()
        );

        // SceneManager is not used here, suppress warning
        (void)gameState;

        if(ogreSound){

            sound->m_sound = ogreSound;
            ogreSound->disable3D(ambient);

            if (autoLoop) {
                // We want to manage looping ourselves
                sound->m_properties.loop = false;
                ogreSound->loop(false);
            }

            m_sounds[entityId].emplace(sound->name(), ogreSound);

            if (not ambient){

                ogreSound->attachToNode(sceneNodeComponent->m_sceneNode);
            }

            sound->m_properties.touch();
        } else {
            std::string msg = "*** SoundSourceSystem::restoreSound() - Sound with name: "+sound->filename()+
                " failed to load!";
            // Logger object?
            std::cout << msg << std::endl;
        }
    }

    EntityFilter<
        SoundSourceComponent,
        Optional<OgreSceneNodeComponent>

    > m_entities = {true};

    //Map of the sounds of all entities for destruction reference
    std::unordered_map<
        EntityId,
        std::unordered_map<std::string, SoundEmitter*>
        > m_sounds;

    GameState* m_gameState = nullptr;

};


SoundSourceSystem::SoundSourceSystem()
  : m_impl(new Implementation())
{

}


SoundSourceSystem::~SoundSourceSystem() {}


void
SoundSourceSystem::activate() {
    System::activate();


    m_impl->restoreAllSounds();
    if (Entity("soundListener", gameState()).exists()){
        auto* sceneNode = static_cast<OgreSceneNodeComponent*>(Entity("soundListener", gameState()).getComponent(OgreSceneNodeComponent::TYPE_ID));
        if (sceneNode != nullptr){
            sceneNode->attachSoundListener();
        }
    }
}


void
SoundSourceSystem::deactivate() {
    if (this->engine()->isSystemTimedShutdown(*this)) {
        System::deactivate();
        m_impl->removeAllSounds();
    }
    else {
        for (auto& value : m_impl->m_entities) {
            std::get<0>(value.second)->m_autoSoundCountdown = 1500;
        }
        this->engine()->timedSystemShutdown(*this, 1500);
    }
}


void
SoundSourceSystem::init(
    GameState* gameState
) {
    System::initNamed("SoundSourceSystem", gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_gameState = gameState;
}


void
SoundSourceSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
SoundSourceSystem::update(
    int milliseconds,
    int
) {
    if (Entity("soundListener", gameState()).exists() && Entity("player", gameState()).exists() ){
        auto* sceneNodeListener = static_cast<OgreSceneNodeComponent*>(Entity("soundListener", gameState()).getComponent(OgreSceneNodeComponent::TYPE_ID));
        auto* sceneNodePlayer = static_cast<OgreSceneNodeComponent*>(Entity("player", gameState()).getComponent(OgreSceneNodeComponent::TYPE_ID));
        if (sceneNodeListener && sceneNodePlayer){
            sceneNodeListener->m_transform.position = sceneNodePlayer->m_transform.position ;
            sceneNodeListener->m_transform.touch();
        }
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        m_impl->removeSoundsForEntity(entityId);
    }
    for (auto& value : m_impl->m_entities.addedEntities()) {
        //Load the songs for any new soundSourceComponent containing entities that have been created
        EntityId entityId = value.first;
        SoundSourceComponent* soundSourceComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        for (const auto& pair : soundSourceComponent->m_sounds) {
            Sound* sound = pair.second.get();
            if (not sound->m_sound) {
                m_impl->restoreSound(
                    entityId,
                    sceneNodeComponent,
                    sound,
                    soundSourceComponent->m_ambientSoundSource,
                    soundSourceComponent->m_autoLoop,
                    this->gameState()
                );
            }
        }
    }
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        SoundSourceComponent* soundSourceComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        for (const auto& pair : soundSourceComponent->m_sounds) {
            Sound* sound = pair.second.get();
            assert(sound->m_sound && "Sound was not intialized");
            if (sound->m_properties.hasChanges()) {
                const auto& properties = sound->m_properties;
                auto ogreSound = sound->m_sound;
                assert(ogreSound && "Sound was not intialized properly");
                ogreSound->loop(properties.loop and not soundSourceComponent->m_autoLoop);
                ogreSound->setVolume(properties.volume * soundSourceComponent->m_volumeMultiplier);
                ogreSound->setMaxDistance(properties.maxDistance);
                ogreSound->setRolloffFactor(properties.rolloffFactor);
                // TODO: do something about properties.referenceDistance and properties.priority
                switch(properties.playState) {
                    case Sound::PlayState::Play:
                        ogreSound->play();
                        break;
                    case Sound::PlayState::Pause:
                        ogreSound->pause();
                        break;
                    case Sound::PlayState::Stop:
                        ogreSound->stop();
                        break;
                    default:
                        // Shut up GCC
                        break;
                }
                sound->m_properties.untouch();
            }
        }
        if (soundSourceComponent->m_shouldInteruptPlaying){
            soundSourceComponent->m_shouldInteruptPlaying = false;
            for (const auto& pair : soundSourceComponent->m_sounds) {
                Sound* sound = pair.second.get();
                if (sound->m_sound) {
                    sound->m_sound->stop();
                }
            }
        }
        if (soundSourceComponent->m_ambientSoundSource.hasChanges()) {
            //Iterate through all existing sounds and set/unset ambience only properties
            for (const auto& pair : soundSourceComponent->m_sounds) {
                Sound* sound = pair.second.get();
                auto ogreSound = sound->m_sound;
                if (ogreSound) {
                    ogreSound->disable3D(soundSourceComponent->m_ambientSoundSource.get() || !sceneNodeComponent);
                    if (soundSourceComponent->m_ambientSoundSource.get()) {
                        sound->stop();
                    }
                }
            }
            soundSourceComponent->m_ambientSoundSource.untouch();
        }
        if (soundSourceComponent->m_autoLoop.hasChanges()) {
            //Iterate through all existing sounds and set/unset ambience only properties
            for (const auto& pair : soundSourceComponent->m_sounds) {
                Sound* sound = pair.second.get();
                auto ogreSound = sound->m_sound;
                if (sound->m_sound) {
                    if (soundSourceComponent->m_ambientSoundSource.get()) {
                        ogreSound->loop(false);
                    }
                    else {
                        ogreSound->loop(sound->m_properties.loop);
                    }
                }
            }
            soundSourceComponent->m_autoLoop.untouch();
        }
        // If the current soundsource is an ambient soundsource
        if (soundSourceComponent->m_autoLoop.get()) {
            //Automatically manage looping of ambient sounds randomly
            // (This would have unintended effects on ambient soundsources not meant for background music
            // and would result in overlap with multiple simultanious ambient soundsource entities
            // a redesign will be necessary if either of those two optiosn are desired)
            soundSourceComponent->m_autoSoundCountdown -= milliseconds;
            if (soundSourceComponent->m_autoSoundCountdown < FADE_TIME) {
                if (soundSourceComponent->m_autoActiveSound && not soundSourceComponent->m_isTransitioningAuto && soundSourceComponent->m_autoSoundCountdown  > 0){
                    soundSourceComponent->m_isTransitioningAuto = true;
                    soundSourceComponent->m_autoActiveSound->m_sound->startFade(false, (soundSourceComponent->m_autoSoundCountdown)/1000.0f);
                }
                if (soundSourceComponent->m_autoSoundCountdown <= 0){
                    // We want to stop the active song instantly, so we can't reply only on the Sound::stop(), we need to call it on the ogresound directly as well.
                    if (soundSourceComponent->m_autoActiveSound) {
                        soundSourceComponent->m_autoActiveSound->m_sound->stop();
                        soundSourceComponent->m_autoActiveSound->stop();
                    }
                    Sound* newSound = nullptr;
                    if (soundSourceComponent->m_queuedSound){
                        //If a sound was queued up to be next, then pick that one
                        newSound = soundSourceComponent->m_queuedSound;
                    }
                    else {
                        //Otherwise pick a sound by random from the avaliable sounds
                        int numOfSounds = soundSourceComponent->m_sounds.size();
                        if (numOfSounds > 0){
                            if (soundSourceComponent->m_queuedSound){
                                newSound = soundSourceComponent->m_queuedSound;
                            }
                            else {
                                do {
                                    int randSoundIndex = Game::instance().engine().rng().getInt(0, numOfSounds-1);
                                    std::unordered_map<std::string, std::unique_ptr<Sound>>::iterator soundPointer
                                        = soundSourceComponent->m_sounds.begin();
                                    for (int i = 0; i < randSoundIndex; ++i)
                                        soundPointer++;
                                    newSound = soundPointer->second.get();
                                } while (newSound == soundSourceComponent->m_autoActiveSound && numOfSounds > 1); //Ensure we don't play the same song twice
                            }
                        }
                    }
                    float soundLength = newSound->m_sound->getAudioLength();
                    // Soundlength will return 0 for a while after initialization (I think it's due to multi-threaded sound init) so we need to handle that
                    if (soundLength > 0){
                        soundSourceComponent->m_autoActiveSound = newSound;
                        soundSourceComponent->m_autoSoundCountdown = newSound->m_sound->getAudioLength()*1000;
                        newSound->play();
                        soundSourceComponent->m_queuedSound = nullptr;
                        //newSound->m_sound->startFade(true, 5000); // In case we want to fade-in themes instead of just playing them.
                    }
                    soundSourceComponent->m_isTransitioningAuto = false;
                }

            }
        }
    }
}

