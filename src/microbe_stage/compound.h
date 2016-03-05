#pragma once

#include <boost/range/adaptor/map.hpp>

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include <luabind/object.hpp>
#include <memory>
#include <OgreCommon.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <unordered_set>

namespace luabind {
class scope;
}


namespace thrive {

using BoostCompoundMapIterator = boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>;
using BoostAbsorbedMapIterator = boost::range_detail::select_first_range<std::unordered_map<CompoundId, float>>;

//static const CompoundId NULL_COMPOUND = 0;

CompoundId
generateCompoundId();

/**
* @brief Component for entities that act as compound particles or agent particles
*        Note that this class is strongly linked with CompoundRegistry.
*/
class CompoundComponent : public Component {
    COMPONENT(Compound)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundComponent()
    * - CompoundComponent::m_compoundId
    * - CompoundComponent::m_potency
    * - CompoundComponent::m_velocity
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compound id
    */
    CompoundId m_compoundId;// = NULL_COMPOUND;

    /**
    * @brief The potency of this particle
    */
    float m_potency = 0.0f;

    /**
    * @brief The current velocity of the particle
    */
    Ogre::Vector3 m_velocity = Ogre::Vector3::ZERO;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

};


/**
* @brief Moves compound particles around
*/
class CompoundMovementSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundMovementSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundMovementSystem();

    /**
    * @brief Destructor
    */
    ~CompoundMovementSystem();

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
    * @brief Updates the system
    */
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
