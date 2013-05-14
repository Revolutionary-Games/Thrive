#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <memory>
#include <OgreString.h>


namespace thrive {

/**
* @brief Component for an entity that has a 3D mesh
*/
class OgreEntityComponent : public Component {
    COMPONENT(Mesh)

public:

    OgreEntityComponent(
        std::string meshName
    );

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_lua shared properties:
    * - \c meshName (string): Properties::meshName
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    const std::string m_meshName;

};


/**
* @brief Updates meshes of OgreEntityComponents
*/
class OgreEntitySystem : public System {

public:

    /**
    * @brief Constructor
    */
    OgreEntitySystem();

    /**
    * @brief Destructor
    */
    ~OgreEntitySystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be an OgreEngine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the graphics engine's data
    *
    * All OgreEntityComponents whose OgreEntityComponent::Properties::meshName has changed
    * will be updated.
    *
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}


