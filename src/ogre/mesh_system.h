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
class MeshComponent : public Component {
    COMPONENT(Mesh)

public:

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_scripts shared properties:
    * - \c meshName (string): Properties::meshName
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Properties to be shared
    */
    struct Properties {
        /**
        * @brief The mesh path
        */
        Ogre::String meshName = "";
    };

    /**
    * @brief Shared properties
    */
    RenderData<Properties>
    m_properties;

};


/**
* @brief Updates meshes of MeshComponents
*/
class MeshSystem : public System {

public:

    /**
    * @brief Constructor
    */
    MeshSystem();

    /**
    * @brief Destructor
    */
    ~MeshSystem();

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
    * All MeshComponents whose MeshComponent::Properties::meshName has changed
    * will be updated.
    *
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

