#pragma once

#include <array>
#include <assert.h>
#include <memory>

#include <iostream>

namespace thrive {

enum class StateGroup {
    RenderInput
};

enum class StateBuffer {
    Stable,     // Read-only
    Latest,     // Read-only
    WorkingCopy // Writable
};

template<StateGroup group>
class SharedState {

public:

    static SharedState&
    instance() {
        static SharedState instance;
        return instance;
    }

    short
    getBufferIndex(
        StateBuffer buffer
    ) const {
        switch (buffer) {
            case StateBuffer::Latest:
                return m_latestBuffer;
            case StateBuffer::Stable:
                return m_stableBuffer;
            case StateBuffer::WorkingCopy:
                return m_workingCopyBuffer;
            default:
                return -1;
        }
    }

    unsigned long
    getBufferVersion(
        StateBuffer buffer
    ) const {
        short index = this->getBufferIndex(buffer);
        assert(index >= 0 && "Invalid buffer specified. How did you manage that?!");
        return m_bufferVersions[index];
    }

    void
    lockStable() {
        assert(m_stableBuffer == -1 && "Double locking stable buffer");
        m_stableBuffer = m_latestBuffer;
    }

    void
    lockWorkingCopy() {
        assert(m_workingCopyBuffer == -1 && "Double locking working copy buffer");
        short oldestBuffer = (m_latestBuffer + 1) % 3;
        short secondOldestBuffer = (m_latestBuffer + 2) % 3;
        if (m_bufferVersions[secondOldestBuffer] < m_bufferVersions[oldestBuffer]) {
            // Oops, we need to swap
            std::swap(oldestBuffer, secondOldestBuffer);
        }
        assert(oldestBuffer != m_latestBuffer and secondOldestBuffer != m_latestBuffer);
        if (oldestBuffer != m_stableBuffer) {
            m_workingCopyBuffer = oldestBuffer;
        }
        else {
            m_workingCopyBuffer = secondOldestBuffer;
        }
    }

    void
    releaseStable() {
        m_stableBuffer = -1;
    }

    void
    releaseWorkingCopy() {
        m_frameIndex += 1;
        m_bufferVersions[m_workingCopyBuffer] = m_frameIndex;
        m_latestBuffer = m_workingCopyBuffer;
        m_workingCopyBuffer = -1;
    }

    void
    reset() {
        m_frameIndex = 0;
        m_latestBuffer = 0;
        m_stableBuffer = -1;
        m_workingCopyBuffer = -1;
        for (int i=0; i < 3; ++i) {
            m_bufferVersions[i] = 0;
        }
    }

private:

    unsigned long m_frameIndex = 0;

    short m_latestBuffer = 0;

    short m_stableBuffer = -1;

    short m_workingCopyBuffer = -1;

    std::array<unsigned long, 3> m_bufferVersions = {{0, 0, 0}};

};

extern template class SharedState<StateGroup::RenderInput>;

////////////////////////////////////////////////////////////////////////////////
// SharedData
////////////////////////////////////////////////////////////////////////////////

template<
    typename Data, 
    StateGroup group, 
    bool updateWorkingCopy = true
>
class SharedData {

public:

    using State = SharedState<group>;

    template<typename... Args>
    SharedData(
        const Args&... args
    ) : m_buffers{{Data{args...}, Data{args...}, Data{args...}}}
    {
    }

    const Data&
    latest() const {
        return this->getBuffer(StateBuffer::Latest);
    }

    const Data&
    stable() const {
        return this->getBuffer(StateBuffer::Stable);
    }

    Data&
    workingCopy() {
        if (updateWorkingCopy) { 
            State& state = State::instance();
            unsigned long latestVersion = state.getBufferVersion(StateBuffer::Latest);
            if (m_workingCopyVersion < latestVersion) {
                this->getBuffer(StateBuffer::WorkingCopy) = this->getBuffer(StateBuffer::Latest);
                m_workingCopyVersion = latestVersion;
            }
        }
        return this->getBuffer(StateBuffer::WorkingCopy);
    }

private:

    Data&
    getBuffer(
        StateBuffer buffer
    ) {
        State& state = State::instance();
        short bufferIndex = state.getBufferIndex(buffer);
        return m_buffers[bufferIndex];
    }

    const Data&
    getBuffer(
        StateBuffer buffer
    ) const {
        State& state = State::instance();
        short bufferIndex = state.getBufferIndex(buffer);
        return m_buffers[bufferIndex];
    }

    unsigned long m_workingCopyVersion = 0;

    std::array<Data, 3> m_buffers;
};


}
