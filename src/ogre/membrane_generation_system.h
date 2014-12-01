#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <memory>
#include <OgreVector3.h>

namespace luabind {
class scope;
}

namespace Ogre {
class Vector3;
}

namespace thrive {

class

/**
* @brief A component for a Ogre scene nodes
*
*/
class MembraneGenerationComponent : public Component {
    COMPONENT(MembraneGenerationComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneGenerationComponent()
    * - MembraneGenerationComponent::sadasdas
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

    /**
    * @brief hrteher
    *
    * @param dwadwa
    *  qfqwfwqf
    *
    */
    void
    dawdaw(
        float dawdaw
    );



    /**
    * @brief sadasdfrf
    */
    TouchableValue<bool> sadasdas = true;


private:


};

class MembraneGenerationSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneGenerationSystem::asdasd
    * - MembraneGenerationSystem::dwqwdwq
    * - MembraneGenerationSystem::asda
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    MembraneGenerationSystem();

    /**
    * @brief Destructor
    */
    ~MembraneGenerationSystem();

    /**
    * @brief Initializes the system
    *
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates system
    */
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

