#include "engine/shared_data.h"

#include <gtest/gtest.h>

using namespace thrive;

using State = SharedState<StateGroup::RenderInput>;

TEST(SharedStateDeathTest, DoubleLockStable) {
    State& state = State::instance();
    state.lockStable();
    EXPECT_DEATH(
        {state.lockStable();}, 
        "Double locking stable buffer"
    );
    state.reset();
}


TEST(SharedStateDeathTest, DoubleLockWorkingCopy) {
    State& state = State::instance();
    state.lockWorkingCopy();
    EXPECT_DEATH(
        {state.lockWorkingCopy();}, 
        "Double locking working copy buffer"
    );
    state.reset();
}


TEST(SharedState, SequentialSwitch) {
    State& state = State::instance();
    EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Latest));
    // Do work
    state.lockWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::WorkingCopy));
    state.releaseWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Latest));
    // Read stuff
    state.lockStable();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Stable));
    state.releaseStable();
    // Clean up
    state.reset();
}


TEST(SharedState, FastWorkingCopy) {
    State& state = State::instance();
    state.lockStable();
    EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Stable));
    // Check if working copy oscillates between buffers
    // 1 and 2 while 0 is locked by stable
    state.lockWorkingCopy();
    state.releaseWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Latest));
    state.lockWorkingCopy();
    state.releaseWorkingCopy();
    EXPECT_EQ(2, state.getBufferIndex(StateBuffer::Latest));
    state.lockWorkingCopy();
    state.releaseWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Latest));
    // Clean up
    state.releaseStable();
    state.reset();
}


TEST(SharedState, FastReader) {
    State& state = State::instance();
    state.lockWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::WorkingCopy));
    // Stable should always get 0 (latest)
    for (int i=0; i < 5; ++i) {
        state.lockStable();
        EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Stable));
        state.releaseStable();
    }
    // Cleanup
    state.releaseWorkingCopy();
    state.reset();
}


TEST(SharedState, Interweaving) {
    State& state = State::instance();
    // Start work
    state.lockWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::WorkingCopy));
    // Start reading
    state.lockStable();
    EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Stable));
    // Stop work and reading
    state.releaseWorkingCopy();
    state.releaseStable();
    // Start next frame work
    state.lockWorkingCopy();
    EXPECT_EQ(2, state.getBufferIndex(StateBuffer::WorkingCopy));
    // Start next frame reading
    state.lockStable();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Stable));
    state.reset();
}


TEST(SharedData, DataTransfer) {
    using Shared = SharedData<int, StateGroup::RenderInput>;
    State& state = State::instance();
    auto data = Shared::create(1);
    // Check for correct initialization
    EXPECT_EQ(1, data->latest());
    // Change some data
    state.lockWorkingCopy();
    EXPECT_EQ(1, data->workingCopy());
    data->workingCopy() = 10;
    // Freeze stable, should be unchanged
    state.lockStable();
    EXPECT_EQ(1, data->stable());
    // Commit working copy, stable should stay unchanged
    state.releaseWorkingCopy();
    EXPECT_EQ(10, data->latest());
    EXPECT_EQ(1, data->stable());
    // Relock stable to get new value
    state.releaseStable();
    state.lockStable();
    EXPECT_EQ(10, data->stable());
}
