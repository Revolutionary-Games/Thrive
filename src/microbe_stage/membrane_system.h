#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include "microbe_stage/membrane.h"

#include <luabind/object.hpp>
#include <memory>
#include <OgreCommon.h>
#include <OgreColourValue.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <unordered_set>


namespace thrive {

class MembraneSystem;

/**
* @brief Emitter for compound particles
*/
class MembraneComponent : public Component {
    COMPONENT(MembraneComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneComponent()
    * - MembraneComponent::m_emissionRadius
    *
    * @return
    */
    static luabind::scope
    luaBindings();


    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    // The colour of the membrane.
    Ogre::ColourValue colour;

    // Gets organelle positions from the .lua file.
    void sendOrganelles(double x, double y);

    // Sets the colour of the membrane.
    void setColour(float red, float green, float blue, float alpha);

    // Gets the position of the closest membrane point
    luabind::object getExternOrganellePos(double x, double y);


private:
    friend class MembraneSystem;
    Membrane m_membrane;
    std::vector<Ogre::Vector3> organellePositions;
    bool wantsMembrane = true;
};



/**
* @brief Spawns compound particles for CompoundEmitterComponent
*/
class MembraneSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    MembraneSystem();

    /**
    * @brief Destructor
    */
    ~MembraneSystem();

    /**
    * @brief Initializes the system
    *
    * @param gameState
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
