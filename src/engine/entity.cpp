#include "engine/entity.h"

#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "game.h"
#include "scripting/luajit.h"

using namespace thrive;


struct Entity::Implementation {

    Implementation(
        EntityId id,
        EntityManager* manager
    ) : m_id(id),
        m_entityManager(manager)
    {
    }

    EntityId m_id = NULL_ENTITY;

    EntityManager* m_entityManager = nullptr;

};


static void
Entity_addComponent(
    Entity* self,
    Component* nakedComponent
) {
    self->addComponent(
        std::unique_ptr<Component>(nakedComponent)
    );
}

void Entity::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Entity>("Entity",

        sol::constructors<sol::types<GameStateData*>,
        sol::types<EntityId, GameStateData*>,
        sol::types<const std::string&, GameStateData*>>(),

        // This should be automatically bound but here we do it explicitly
        sol::meta_function::equal_to, &Entity::operator==,

        "addComponent", &Entity_addComponent,
        "destroy", &Entity::destroy,
        "exists", &Entity::exists,
        "getComponent", &Entity::getComponent,
        "isVolatile", &Entity::isVolatile,
        "removeComponent", &Entity::removeComponent,
        // prefer to call LuaEngine:transferEntityGameState. This is slow
        //"transfer", &Entity::transfer,
        "setVolatile", &Entity::setVolatile,
        "stealName", &Entity::stealName,
        "addChild", &Entity::addChild,
        "hasChildren", &Entity::hasChildren,
        "id", &Entity::id
        
    );
}

static EntityManager*
getEntityManager(
    GameStateData* gameState
) {
    if(gameState == nullptr)
        throw std::runtime_error("Entity constructor: getEntityManager can't get "
            "manager from null gameState");
    
    return gameState->entityManager();
}


Entity::Entity(
    GameStateData* gameState
) : Entity(getEntityManager(gameState)->generateNewId(), gameState)
{

}


Entity::Entity(
    EntityId id,
    GameStateData* gameState
) : m_impl(new Implementation(id, getEntityManager(gameState)))
{
}


Entity::Entity(
    const std::string& name,
    GameStateData* gameState
) : Entity(getEntityManager(gameState)->getNamedId(name), gameState)
{
}


Entity::Entity(
    const Entity& other
) : m_impl(new Implementation(other.m_impl->m_id, other.m_impl->m_entityManager))
{
}


Entity::~Entity() {}


bool
Entity::operator == (
    const Entity& other
) const {
    return
        (m_impl->m_entityManager == other.m_impl->m_entityManager) and
        (m_impl->m_id == other.m_impl->m_id)
    ;
}


Entity&
Entity::operator = (
    const Entity& other
) {
    if (this != &other) {
        m_impl->m_id = other.m_impl->m_id;
        m_impl->m_entityManager = other.m_impl->m_entityManager;
    }
    return *this;
}


void
Entity::addComponent(
    std::unique_ptr<Component> component
) {
    m_impl->m_entityManager->addComponent(
        m_impl->m_id,
        std::move(component)
    );
}


void
Entity::destroy() {
    m_impl->m_entityManager->removeEntity(m_impl->m_id);
}


bool
Entity::exists() const {
    return m_impl->m_entityManager->exists(m_impl->m_id);
}


Component*
Entity::getComponent(
    ComponentTypeId typeId
) {
    return m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
}


bool
Entity::hasComponent(
    ComponentTypeId typeId
) {
    Component* component = m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
    return component != nullptr;
}


EntityId
Entity::id() const {
    return m_impl->m_id;
}


bool
Entity::isVolatile() const {
    return m_impl->m_entityManager->isVolatile(m_impl->m_id);
}


void
Entity::removeComponent(
    ComponentTypeId typeId
) {
    m_impl->m_entityManager->removeComponent(
        m_impl->m_id,
        typeId
    );
}

Entity
Entity::transfer(
    GameStateData* newGameStateData
) {

    EntityId newID = Game::instance().engine().transferEntityGameState(m_impl->m_id,
        m_impl->m_entityManager, newGameStateData);
    
    return Entity(newID, newGameStateData);
}

void
Entity::setVolatile(
    bool isVolatile
) {
    m_impl->m_entityManager->setVolatile(m_impl->m_id, isVolatile);
}

void
Entity::addChild(
    Entity& child
) {
    m_impl->m_entityManager->addChild(child.m_impl->m_id, m_impl->m_id);
}

bool
Entity::hasChildren() const {
    return m_impl->m_entityManager->hasChildren(m_impl->m_id);
}

void
Entity::stealName(
    const std::string& name
) {
    m_impl->m_entityManager->stealName(m_impl->m_id, name);
}

