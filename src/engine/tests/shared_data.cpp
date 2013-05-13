#include "engine/shared_data.h"

#include <gtest/gtest.h>

using namespace thrive;

using State = RenderState;

TEST(SharedStateDeathTest, DoubleLockStable) {
    State& state = State::instance();
    state.reset();
    state.lockStable();
    EXPECT_DEATH(
        {state.lockStable();}, 
        "Double locking stable buffer"
    );
    state.reset();
}


TEST(SharedStateDeathTest, DoubleLockWorkingCopy) {
    State& state = State::instance();
    state.reset();
    state.lockWorkingCopy();
    EXPECT_DEATH(
        {state.lockWorkingCopy();}, 
        "Double locking working copy buffer"
    );
    state.reset();
}


TEST(SharedState, SequentialSwitch) {
    State& state = State::instance();
    state.reset();
    EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Latest));
    // Do work
    state.lockWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::WorkingCopy));
    state.releaseWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Latest));
    // Read stuff
    state.lockStable();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::Stable));
    state.reset();
}


TEST(SharedState, FastWorkingCopy) {
    State& state = State::instance();
    state.reset();
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
    state.reset();
}


TEST(SharedState, FastReader) {
    State& state = State::instance();
    state.reset();
    state.lockWorkingCopy();
    EXPECT_EQ(1, state.getBufferIndex(StateBuffer::WorkingCopy));
    // Stable should always get 0 (latest)
    for (int i=0; i < 5; ++i) {
        state.lockStable();
        EXPECT_EQ(0, state.getBufferIndex(StateBuffer::Stable));
        state.releaseStable();
    }
    // Cleanup
    state.reset();
}


TEST(SharedState, Interweaving) {
    State& state = State::instance();
    state.reset();
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


TEST(SharedData, UpdateWorkingCopy) {
    State& state = State::instance();
    state.reset();
    RenderData<int> data(1);
    // Change some data
    state.lockWorkingCopy();
    data.workingCopy() = 10;
    data.touch();
    state.releaseWorkingCopy();
    // Just to make sure
    EXPECT_EQ(10, data.latest());
    // Relock working copy, should be 10
    state.lockWorkingCopy();
    EXPECT_EQ(10, data.workingCopy());
    state.reset();
}


TEST(SharedData, DataTransfer) {
    State& state = State::instance();
    state.reset();
    RenderData<int> data(1);
    // Check for correct initialization
    EXPECT_EQ(1, data.latest());
    // Change some data
    state.lockWorkingCopy();
    EXPECT_EQ(1, data.workingCopy());
    data.workingCopy() = 10;
    // Freeze stable, should be unchanged
    state.lockStable();
    EXPECT_EQ(1, data.stable());
    // Commit working copy, stable should stay unchanged
    state.releaseWorkingCopy();
    EXPECT_EQ(10, data.latest());
    EXPECT_EQ(1, data.stable());
    // Relock stable to get new value
    state.releaseStable();
    state.lockStable();
    EXPECT_EQ(10, data.stable());
    state.reset();
}


TEST(SharedQueue, Push) {
    State& state = State::instance();
    state.reset();
    RenderQueue<int> queue;
    // Push some values
    state.lockWorkingCopy();
    queue.push(1);
    queue.push(2);
    // Freeze stable, should be empty
    state.lockStable();
    EXPECT_EQ(0, queue.entries().size());
    // Commit working copy, stable should stay unchanged
    state.releaseWorkingCopy();
    EXPECT_EQ(0, queue.entries().size());
    // Relock stable to get new values
    state.releaseStable();
    state.lockStable();
    EXPECT_EQ(2, queue.entries().size());
    EXPECT_EQ(1, queue.entries().front());
    EXPECT_EQ(2, queue.entries().back());
    // Push some more values
    state.lockWorkingCopy();
    queue.push(3);
    queue.push(4);
    queue.push(5);
    state.releaseWorkingCopy();
    // Stable should remain unchanged
    EXPECT_EQ(2, queue.entries().size());
    // Relock stable
    state.releaseStable();
    state.lockStable();
    EXPECT_EQ(3, queue.entries().size());
    EXPECT_EQ(3, queue.entries().front());
    EXPECT_EQ(5, queue.entries().back());
    // One more relock, queue should be empty
    state.releaseStable();
    state.lockStable();
    EXPECT_EQ(0, queue.entries().size());
    state.reset();
}
