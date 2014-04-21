#include "sound_source_system.h"

#include "engine/component_factory.h"
#include "engine/entity_filter.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/serialization.h"
#include "engine/rng.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include "game.h"

#include <OgreOggISound.h>
#include <OgreOggSoundManager.h>


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
    m_properties.maxDistance = storage.get<float>("maxDistance", -1.0f);
    m_properties.rolloffFactor = storage.get<float>("rolloffFactor", -1.0f);
    m_properties.referenceDistance = storage.get<float>("referenceDistance", 100.0f);
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
        .def("queueSound", &SoundSourceComponent::queueSound)
        .def("interpose", &SoundSourceComponent::interpose)
        .property("ambientSoundSource", SoundSourceComponent_getAmbientSoundSource, SoundSourceComponent_setAmbientSoundSource)
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
    if (iterator != m_sounds.end()) {
        m_removedSounds.push_back(iterator->second.get());
        m_sounds.erase(iterator);
    }
}


void
SoundSourceComponent::interpose(
    std::string name,
    int fadeTime
){
    queueSound(name);
    m_ambientSoundCountdown = fadeTime;
}


void
SoundSourceComponent::queueSound(
    std::string name
){
    m_queuedSound = m_sounds.at(name).get();
}


void
SoundSourceComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_ambientSoundSource = storage.get<bool>("ambientSoundSource");
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
        EntityManager& entityManager = Game::instance().engine().currentGameState()->entityManager();
        SoundSourceComponent* soundSource = static_cast<SoundSourceComponent*>(entityManager.getComponent(entityId, SoundSourceComponent::TYPE_ID));
        for (const auto& pair : soundSource->m_sounds) {
            Sound* sound = pair.second.get();
            sound->m_sound = nullptr;
        }
        for (const auto& pair : m_sounds[entityId]) {
            OgreOggSound::OgreOggISound* sound = pair.second;
            this->removeSound(sound);
        }
        m_sounds[entityId].clear();
    }

    void
    removeSound(
        OgreOggSound::OgreOggISound* sound
    ) {
        auto& soundManager = OgreOggSound::OgreOggSoundManager::getSingleton();
        if (sound) {
            Ogre::SceneNode* sceneNode = sound->getParentSceneNode();
            if (sceneNode){
                sceneNode->detachObject(sound);
            }
            soundManager.destroySound(sound);
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
                    soundSourceComponent->m_ambientSoundSource
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
        bool ambient
    ) {
        static const bool STREAM = true; //Streaming sound from file
        static const bool PREBUFFER = true; //Attaches soundsource on creation
        // 3D sounds should not be attempted loaded before scenenodes are created
        if (not ambient && not sceneNodeComponent->m_sceneNode){
            return;
        }
        auto& soundManager = OgreOggSound::OgreOggSoundManager::getSingleton();
        OgreOggSound::OgreOggISound* ogreSound = soundManager.createSound(
            sound->name(),
            sound->filename(),
            STREAM,
            sound->m_properties.loop,
            PREBUFFER
        );
        if (ogreSound) {
            sound->m_sound = ogreSound;
            ogreSound->disable3D(ambient);
            if (ambient) {
                // We want to manage ambient sound looping ourselves
                sound->m_properties.loop = false;
                ogreSound->loop(false);
            }
            m_sounds[entityId].emplace(sound->name(), ogreSound);
            if (sceneNodeComponent){
                sceneNodeComponent->m_sceneNode->attachObject(ogreSound);
            }
            sound->m_properties.touch();
        }
        else {
            Ogre::String msg = "*** SoundSourceSystem::restoreSound() - Sound with name: "+sound->filename()+" failed to load!";
            Ogre::LogManager::getSingleton().logMessage(msg);
        }
    }

    EntityFilter<
        SoundSourceComponent,
        Optional<OgreSceneNodeComponent>

    > m_entities = {true};

    //Map of the sounds of all entities for destruction reference
    std::unordered_map<
        EntityId,
        std::unordered_map<std::string, OgreOggSound::OgreOggISound*>
    > m_sounds;

};


SoundSourceSystem::SoundSourceSystem()
  : m_impl(new Implementation())
{

}


SoundSourceSystem::~SoundSourceSystem() {}


void
SoundSourceSystem::activate() {
    System::activate();
    auto& soundManager = OgreOggSound::OgreOggSoundManager::getSingleton();
    soundManager.setSceneManager(this->gameState()->sceneManager());
    m_impl->restoreAllSounds();
    if (Entity("player", gameState()).exists()){
        static_cast<OgreSceneNodeComponent*>(Entity("player", gameState()).getComponent(OgreSceneNodeComponent::TYPE_ID))->attachSoundListener();
    }
}


void
SoundSourceSystem::deactivate() {
    System::deactivate();
    auto& soundManager = OgreOggSound::OgreOggSoundManager::getSingleton();
    m_impl->removeAllSounds();
    for (auto& value : m_impl->m_entities) {
        std::get<0>(value.second)->m_ambientSoundCountdown = 0;
    }
    soundManager.setSceneManager(nullptr);
}


void
SoundSourceSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
SoundSourceSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
SoundSourceSystem::update(int milliseconds) {
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
            // If soun
            if (not sound->m_sound) {
                m_impl->restoreSound(
                    entityId,
                    sceneNodeComponent,
                    sound,
                    soundSourceComponent->m_ambientSoundSource
                );
            }
        }
    }
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        SoundSourceComponent* soundSourceComponent = std::get<0>(value.second);
        for (const auto& pair : soundSourceComponent->m_sounds) {
            Sound* sound = pair.second.get();
            assert(sound->m_sound && "Sound was not intialized");
            if (sound->m_properties.hasChanges()) {
                const auto& properties = sound->m_properties;
                OgreOggSound::OgreOggISound* ogreSound = sound->m_sound;
                ogreSound->loop(properties.loop and not soundSourceComponent->m_ambientSoundSource);
                ogreSound->setVolume(properties.volume * soundSourceComponent->m_volumeMultiplier);
                ogreSound->setMaxDistance(properties.maxDistance);
                ogreSound->setRolloffFactor(properties.rolloffFactor);
                ogreSound->setReferenceDistance(properties.referenceDistance);
                ogreSound->setPriority(properties.priority);
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
        if (soundSourceComponent->m_ambientSoundSource.hasChanges()) {
            //Iterate through all existing sounds and set/unset ambience only properties
            for (const auto& pair : soundSourceComponent->m_sounds) {
                Sound* sound = pair.second.get();
                OgreOggSound::OgreOggISound* ogreSound = sound->m_sound;
                ogreSound->disable3D(soundSourceComponent->m_ambientSoundSource.get());
                if (soundSourceComponent->m_ambientSoundSource.get()) {
                    ogreSound->loop(false);
                    sound->stop();
                }
                else {
                    ogreSound->loop(sound->m_properties.loop);
                }
            }
            soundSourceComponent->m_ambientSoundSource.untouch();
        }
        // If the current soundsource is an ambient soundsource
        if (soundSourceComponent->m_ambientSoundSource.get()) {
            //Automatically manage looping of ambient sounds randomly
            // (This would have unintended effects on ambient soundsources not meant for background music
            // and would result in overlap with multiple simultanious ambient soundsource entities
            // a redesign will be necessary if either of those two optiosn are desired)
            soundSourceComponent->m_ambientSoundCountdown -= milliseconds;
            if (soundSourceComponent->m_ambientSoundCountdown < FADE_TIME) {
                if (soundSourceComponent->m_ambientActiveSound && not soundSourceComponent->m_isTransitioningAmbient && soundSourceComponent->m_ambientSoundCountdown  > 0){
                    soundSourceComponent->m_isTransitioningAmbient = true;
                    soundSourceComponent->m_ambientActiveSound->m_sound->startFade(false, (soundSourceComponent->m_ambientSoundCountdown)/1000.0f);
                }
                if (soundSourceComponent->m_ambientSoundCountdown <= 0){
                    // We want to stop the active song instantly, so we can't reply only on the Sound::stop(), we need to call it on the ogresound directly as well.
                    if (soundSourceComponent->m_ambientActiveSound) {
                        soundSourceComponent->m_ambientActiveSound->m_sound->stop();
                        soundSourceComponent->m_ambientActiveSound->stop();
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
                                } while (newSound == soundSourceComponent->m_ambientActiveSound && numOfSounds > 1); //Ensure we don't play the same song twice
                            }
                        }
                    }
                    float soundLength = newSound->m_sound->getAudioLength();
                    // Soundlength will return 0 for a while after initialization (I think it's due to multi-threaded sound init) so we need to handle that
                    if (soundLength > 0){
                        soundSourceComponent->m_ambientActiveSound = newSound;
                        soundSourceComponent->m_ambientSoundCountdown = newSound->m_sound->getAudioLength()*1000;
                        newSound->play();
                        soundSourceComponent->m_queuedSound = nullptr;
                        //newSound->m_sound->startFade(true, 5000); // In case we want to fade-in themes instead of just playing them.
                    }
                    soundSourceComponent->m_isTransitioningAmbient = false;
                }

            }
        }
    }
}

