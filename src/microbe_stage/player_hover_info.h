#pragma once

namespace thrive {

class CellStageWorld;

//! A system that manages reading what the player is hovering over and sending
//! it to the GUI
class PlayerHoverInfoSystem {
public:
    static constexpr auto RUN_EVERY_MS = 100;

    void
        Run(CellStageWorld& world);

private:
    // Used to run every
    int passed = 0;
};
} // namespace thrive
