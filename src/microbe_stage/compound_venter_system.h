#pragma once
#include "engine/component_types.h"
#include "engine/typedefs.h"

#include <Entities/Component.h>
#include <Entities/System.h>
//#include <Entities/Components.h>
#include "process_system.h"
#include <unordered_map>
#include <vector>

namespace Leviathan {
class GameWorld;
}

namespace thrive {

class CellStageWorld;
class CompoundVenterComponent : public Leviathan::Component {
public:
    CompoundVenterComponent();

    double storageSpace;
    double storageSpaceOccupied;
    float x, y;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(CompoundVenterComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::COMPOUND_VENTER);

    void
        ventCompound(Leviathan::Position& pos,
            CompoundId ourCompound,
            CellStageWorld& world);
};

class CompoundVenterSystem
    : public Leviathan::System<std::tuple<CompoundBagComponent&,
          CompoundVenterComponent&,
          Leviathan::Position&>> {
public:
    /**
     * @brief Updates the system
     * @todo Make it releases a specific amount of compounds each second
     */
    void
        Run(CellStageWorld& world);

    void
        CreateNodes(
            const std::vector<std::tuple<CompoundBagComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<CompoundVenterComponent*, ObjectID>>&
                seconddata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                thirdData,
            const ComponentHolder<CompoundBagComponent>& firstholder,
            const ComponentHolder<CompoundVenterComponent>& secondholder,
            const ComponentHolder<Leviathan::Position>& thirdHolder)
    {
        TupleCachedComponentCollectionHelper(CachedComponents, firstdata,
            seconddata, thirdData, firstholder, secondholder, thirdHolder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<CompoundBagComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<CompoundVenterComponent*, ObjectID>>&
                seconddata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                thirdData)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
        CachedComponents.RemoveBasedOnKeyTupleList(thirdData);
    }

protected:
private:
    static constexpr double TIME_SCALING_FACTOR = 1000;
};
} // namespace thrive