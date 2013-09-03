#include "scripting/script_system_updater.h"

#include "engine/engine.h"
#include "scripting/luabind.h"

#include <list>

using namespace thrive;


struct ScriptSystemUpdater::Implementation {

    std::list<std::shared_ptr<System>> m_systems;

};


ScriptSystemUpdater::ScriptSystemUpdater()
  : m_impl(new Implementation())
{
}


ScriptSystemUpdater::~ScriptSystemUpdater() {}


void
ScriptSystemUpdater::addSystem(
    std::shared_ptr<System> system
) {
    m_impl->m_systems.push_back(system);
}


void
ScriptSystemUpdater::initSystems(
    Engine* engine
) {
    for (const auto& system : m_impl->m_systems) {
        try {
            system->init(engine);
        }
        catch (const luabind::error& e) {
            luabind::object error_msg(luabind::from_stack(
                e.state(),
                -1
            ));
            // TODO: Log error
            std::cerr << error_msg << std::endl;
        }
        catch(const std::exception& e) {
            std::cerr << "Unexpected exception during Lua call:" << e.what() << std::endl;
        }
    }
}


void
ScriptSystemUpdater::shutdownSystems() {
    for (const auto& system : m_impl->m_systems) {
        try {
            system->shutdown();
        }
        catch (const luabind::error& e) {
            luabind::object error_msg(luabind::from_stack(
                e.state(),
                -1
            ));
            // TODO: Log error
            std::cerr << error_msg << std::endl;
        }
        catch(const std::exception& e) {
            std::cerr << "Unexpected exception during Lua call:" << e.what() << std::endl;
        }
    }
}


const std::list<std::shared_ptr<System>>&
ScriptSystemUpdater::systems() {
    return m_impl->m_systems;
}


void
ScriptSystemUpdater::update(int milliseconds) {
    for (const auto& system : m_impl->m_systems) {
        if (system->active()) {
            try {
                system->update(milliseconds);
            }
            catch (const luabind::error& e) {
                luabind::object error_msg(luabind::from_stack(
                    e.state(),
                    -1
                ));
                // TODO: Log error
                std::cerr << error_msg << std::endl;
            }
            catch(const std::exception& e) {
                std::cerr << "Unexpected exception during Lua call:" << e.what() << std::endl;
            }
        }
    }
}


