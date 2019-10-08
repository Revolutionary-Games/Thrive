#pragma once

#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>
#include <json/json.h>
#include <unordered_map>

namespace thrive {

class CellStageWorld;
class MembraneComponent;

//! \brief A system that manages reading what the player is hovering over and
//! sending it to the GUI
//!
//! Membranes and positions are used to find cells that are hovered over
class PlayerHoverInfoSystem
    : public Leviathan::System<
          std::tuple<MembraneComponent&, Leviathan::Position&>> {
public:
    PlayerHoverInfoSystem();

    static constexpr auto RUN_EVERY_MS = 100;

    void
        Run(CellStageWorld& world);

    void
        CreateNodes(const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                        firstdata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                seconddata,
            const ComponentHolder<MembraneComponent>& firstholder,
            const ComponentHolder<Leviathan::Position>& secondholder)
    {
        TupleCachedComponentCollectionHelper(
            CachedComponents, firstdata, seconddata, firstholder, secondholder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                seconddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
    }

private:
    // Used to run every RUN_EVERY_MS
    int passed = 0;
    std::unique_ptr<Json::StreamWriter> writer;
};
} // namespace thrive
