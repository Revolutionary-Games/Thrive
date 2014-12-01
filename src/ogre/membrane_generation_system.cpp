#include "ogre/membrane_generation_system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luabind.h"

using namespace thrive;

luabind::scope
MembraneGenerationComponent::luaBindings() {
    using namespace luabind;
    return class_<MembraneGenerationComponent, Component>("MembraneGenerationComponent")
        .enum_("ID") [
            value("TYPE_ID", MembraneGenerationComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &MembraneGenerationComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("dqwwqd", &MembraneGenerationComponent::dwqdqwd)
        .def_readonly("aasasas", &MembraneGenerationComponent::assasa)
    ;
}


void
MembraneGenerationComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    asdasdfew = storage.get<Ogre::String>("adsadasdfweq");
}


StorageContainer
MembraneGenerationComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Quaternion>("sadasd", asdasdn);
    return storage;
}

void
MembraneGenerationComponent::asdasdas(
    float sadasdas
) {
}

REGISTER_COMPONENT(MembraneGenerationComponent)









luabind::scope
MembraneGenerationSystem::luaBindings() {
    using namespace luabind;
    return class_<MembraneGenerationSystem, System>("MembraneGenerationSystem")
        .def(constructor<>())
    ;
}


struct MembraneGenerationSystem::Implementation {


};


MembraneGenerationSystem::MembraneGenerationSystem()
  : m_impl(new Implementation())
{
}


MembraneGenerationSystem::~MembraneGenerationSystem() {}


void
MembraneGenerationSystem::init(
    GameState* gameState
) {
    System::init(gameState);
}


void
MembraneGenerationSystem::shutdown() {
    System::shutdown();
}


void
MembraneGenerationSystem::update(
    int renderTime,
    int logicTime
) {
}


