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

namespace thrive {

using BoostCompoundMapIterator = boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>;
using BoostAbsorbedMapIterator = boost::range_detail::select_first_range<std::unordered_map<CompoundId, float>>;

/**
* @brief Absorbs compound particles
*/
class CompoundAbsorberComponent : public Component {
    COMPONENT(CompoundAbsorber)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundAbsorberComponent::absorbedCompoundAmount
    * - CompoundAbsorberComponent::getAbsorbedCompounds
    * - CompoundAbsorberComponent::canAbsorbCompound
    * - CompoundAbsorberComponent::setCanAbsorbCompound
    * - CompoundAbsorberComponent::setAbsorbtionCapacity
    * - CompoundAbsorberComponent::enable
    * - CompoundAbsorberComponent::disable
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compounds absorbed in the last time step
    */
    std::unordered_map<CompoundId, float> m_absorbedCompounds;

    /**
    * @brief Whether a particular compound id can be absorbed
    */
    std::unordered_set<CompoundId> m_canAbsorbCompound;

    /**
    * @brief Whether anything can be absorbed
    */
    bool m_enabled = true;

    /**
    * @brief The amount of compound volume that can be absorbed
    */
    double m_absorbtionCapacity = 0;

    /**
    * @brief The absorbed amount in the last time step
    *
    * @param id
    *   The compound id to get the amount for
    *
    * @return
    */
    float
    absorbedCompoundAmount(
        CompoundId id
    ) const;

    /**
    * @brief Whether an compound can be absorbed
    *
    * @param id
    *   The compound id to check
    *
    * @return
    */
    bool
    canAbsorbCompound(
        CompoundId id
    ) const;

    /**
    * @brief Sets the absorbtion capacity
    *
    * @param capacity
    *   The new capacity
    */
    void
    setAbsorbtionCapacity(
        double capacity
    );

    /**
    * Sets m_enabled to true
    */
    void
    enable();

    /**
    * Sets m_enabled to false
    */
    void
    disable();

    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief Sets the amount of absorbed compounds
    *
    * Use this for e.g. resetting the absorbed amount down
    * to zero.
    *
    * @param id
    *   The compound id to change the amount for
    * @param amount
    *   The new amount
    */
    void
    setAbsorbedCompoundAmount(
        CompoundId id,
        float amount
    );

    BoostAbsorbedMapIterator
    getAbsorbedCompounds();

    /**
    * @brief Sets whether an compound can be absorbed
    *
    * @param id
    *   The compound id to set the flag for
    * @param canAbsorb
    *   Whether to absorb the compound
    */
    void
    setCanAbsorbCompound(
        CompoundId id,
        bool canAbsorb
    );

    StorageContainer
    storage() const override;

};


/**
* @brief Despawns compounds for CompoundAbsorberComponent
*/
class CompoundAbsorberSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundAbsorberSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundAbsorberSystem();

    /**
    * @brief Destructor
    */
    ~CompoundAbsorberSystem();

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
