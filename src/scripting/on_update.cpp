#include "scripting/on_update.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "game.h"
#include "scripting/script_engine.h"
#include "scripting/luabind.h"


using namespace thrive;

luabind::scope
OnUpdateComponent::luaBindings() {
    using namespace luabind;
    return class_<OnUpdateComponent, Component, std::shared_ptr<Component>>("OnUpdateComponent")
        .scope[
            def("TYPE_NAME", &OnUpdateComponent::TYPE_NAME),
            def("TYPE_ID", &OnUpdateComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("callback", &OnUpdateComponent::m_callback)
    ;
}

REGISTER_COMPONENT(OnUpdateComponent)

////////////////////////////////////////////////////////////////////////////////
// OnUpdateSystem
////////////////////////////////////////////////////////////////////////////////

struct OnUpdateSystem::Implementation {

    EntityFilter<OnUpdateComponent> m_entities;

};


OnUpdateSystem::OnUpdateSystem() 
  : m_impl(new Implementation())
{
}


OnUpdateSystem::~OnUpdateSystem() {}


void
OnUpdateSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OnUpdateSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
OnUpdateSystem::update(
    int milliseconds
) {
    for (auto& value : m_impl->m_entities) {
        OnUpdateComponent* component = std::get<0>(value.second);
        luabind::object& callback = component->m_callback;
        if (callback.is_valid()) {
            EntityId entityId = value.first;
            try {
                callback(entityId, milliseconds);
            }
            catch(const luabind::error& e) {
                luabind::object error_msg(luabind::from_stack(
                    e.state(),
                    -1
                ));
                // TODO: Log error
                std::cerr << error_msg << std::endl;
            }
            catch(const std::exception& e) {
                std::cerr << "Unexpected exception during Lua callback:" << e.what() << std::endl;
            }
        }
    }
}



