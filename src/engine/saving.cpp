#include "engine/saving.h"

#include "engine/component_collection.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/serialization.h"

#include <string>

using namespace thrive;

struct SaveSystem::Implementation {

    std::string m_filename;

};


SaveSystem::SaveSystem()
  : m_impl(new Implementation())
{
}


SaveSystem::~SaveSystem() {}


void
SaveSystem::save(
    std::string filename
) {
    m_impl->m_filename = filename;
    this->setActive(true);
}


void
SaveSystem::update(int) {
    StorageContainer entities;
    try {
        entities = this->engine()->entityManager().storage();
    }
    catch (const luabind::error& e) {
        luabind::object error_msg(luabind::from_stack(
            e.state(),
            -1
        ));
        // TODO: Log error
        std::cerr << error_msg << std::endl;
        throw;
    }
    StorageContainer savegame;
    savegame.set("entities", std::move(entities));
    std::ofstream stream(m_impl->m_filename);
    stream << savegame;
    this->setActive(false);
}


////////////////////////////////////////////////////////////////////////////////
// LoadSystem
////////////////////////////////////////////////////////////////////////////////

struct LoadSystem::Implementation {

    std::string m_filename;

};


LoadSystem::LoadSystem()
  : m_impl(new Implementation())
{
}


LoadSystem::~LoadSystem() {}


void
LoadSystem::load(
    std::string filename
) {
    m_impl->m_filename = filename;
    this->setActive(true);
}


void
LoadSystem::update(int) {
    EntityManager& entityManager = this->engine()->entityManager();
    entityManager.clear();
    std::ifstream stream(m_impl->m_filename);
    StorageContainer savegame;
    stream >> savegame;
    StorageContainer entities = savegame.get<StorageContainer>("entities");
    std::list<std::string> typeNames = entities.keys();
    try {
        this->engine()->entityManager().restore(
            entities,
            this->engine()->componentFactory()
        );
    }
    catch (const luabind::error& e) {
        luabind::object error_msg(luabind::from_stack(
            e.state(),
            -1
        ));
        // TODO: Log error
        std::cerr << error_msg << std::endl;
        throw;
    }
    this->setActive(false);
}
