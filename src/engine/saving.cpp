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
    EntityManager& entityManager = this->engine()->entityManager();
    StorageContainer entities;
    for (ComponentTypeId typeId : entityManager.nonEmptyCollections()) {
        ComponentCollection& collection = entityManager.getComponentCollection(typeId);
        const auto& components = collection.components();
        StorageList componentList;
        componentList.reserve(components.size());
        std::string typeName = "";
        for (const auto& pair : components) {
            if (typeName.empty()) {
                typeName = pair.second->typeName();
            }
            componentList.append(pair.second->storage());
        }
        entities.set(typeName, std::move(componentList));
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
    for (const std::string& typeName : typeNames) {
        StorageList componentStorages = entities.get<StorageList>(typeName);
        for (const StorageContainer& componentStorage : componentStorages) {
            auto component = this->engine()->componentFactory().load(typeName, componentStorage);
            EntityId owner = component->owner();
            entityManager.addComponent(owner, std::move(component));
        }
    }
    this->setActive(false);
}
