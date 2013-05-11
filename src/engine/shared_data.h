#pragma once

#include "engine/typedefs.h"

#include <array>
#include <assert.h>
#include <boost/thread.hpp>
#include <deque>
#include <memory>
#include <unordered_set>

#include <iostream>

namespace thrive {

enum class StateBuffer {
    Stable,     // Read-only
    Latest,     // Read-only
    WorkingCopy // Writable
};


namespace detail {
    template<ThreadId Writer, ThreadId Reader>
    struct SharedDataBase {
        virtual void updateBuffer(short bufferIndex) = 0;
    };
}

template<ThreadId Writer, ThreadId Reader>
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

    FrameIndex
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
        for (auto sharedData : m_registeredSharedData) {
            sharedData->updateBuffer(m_workingCopyBuffer);
        }
    }

    void
    registerSharedData(
        detail::SharedDataBase<Writer, Reader>* sharedData
    ) {
        m_registeredSharedData.insert(sharedData);
    }

    void
    releaseStable() {
        m_stableFrame += 1;
        m_stableBuffer = -1;
    }

    void
    releaseWorkingCopy() {
        m_workingCopyFrame += 1;
        m_bufferVersions[m_workingCopyBuffer] = m_workingCopyFrame;
        m_latestBuffer = m_workingCopyBuffer;
        m_workingCopyBuffer = -1;
    }

    void
    reset() {
        m_workingCopyFrame = 0;
        m_stableFrame = 0;
        m_latestBuffer = 0;
        m_stableBuffer = -1;
        m_workingCopyBuffer = -1;
        for (int i=0; i < 3; ++i) {
            m_bufferVersions[i] = 0;
        }
    }

    FrameIndex
    stableFrame() const {
        return m_stableFrame;
    }

    FrameIndex
    workingCopyFrame() const {
        return m_workingCopyFrame;
    }

    void
    unregisterSharedData(
        detail::SharedDataBase<Writer, Reader>* sharedData
    ) {
        m_registeredSharedData.erase(sharedData);
    }

private:

    std::array<FrameIndex, 3> m_bufferVersions = {{0, 0, 0}};

    short m_latestBuffer = 0;

    short m_stableBuffer = -1;

    FrameIndex m_stableFrame = 0;

    std::unordered_set<detail::SharedDataBase<Writer, Reader>*> m_registeredSharedData;

    short m_workingCopyBuffer = -1;

    FrameIndex m_workingCopyFrame = 0;

};


template<
    typename State,
    StateBuffer Buffer
>
class StateLock { };


template<typename State>
class StateLock<State, StateBuffer::Stable> {

public:

    StateLock() {
        State::instance().lockStable();
    };

    ~StateLock() {
        State::instance().releaseStable();
    }
};

template<typename State>
class StateLock<State, StateBuffer::WorkingCopy> {

public:

    StateLock() {
        State::instance().lockWorkingCopy();
    };

    ~StateLock() {
        State::instance().releaseWorkingCopy();
    }
};

////////////////////////////////////////////////////////////////////////////////
// SharedData
////////////////////////////////////////////////////////////////////////////////

template<
    typename Data_, 
    ThreadId Writer, ThreadId Reader, 
    bool copyBuffers = true
>
class SharedData : public detail::SharedDataBase<Writer, Reader> {

public:

    using Data = Data_;

    using State = SharedState<Writer, Reader>;

    template<typename... Args>
    SharedData(
        const Args&... args
    ) : m_buffers{{Data{args...}, Data{args...}, Data{args...}}}
    {
        State::instance().registerSharedData(this);
    }

    SharedData(const SharedData&) = delete;

    ~SharedData() {
        State::instance().unregisterSharedData(this);
    }

    SharedData& operator= (const SharedData&) = delete;

    const Data&
    latest() const {
        return this->getBuffer(StateBuffer::Latest);
    }

    const Data&
    stable() const {
        return this->getBuffer(StateBuffer::Stable);
    }

    FrameIndex
    stableVersion() const {
        State& state = State::instance();
        short bufferIndex = state.getBufferIndex(StateBuffer::Stable);
        return m_bufferVersions[bufferIndex];
    }

    void
    touch() {
        m_touchedVersion += 1;
    }

    void
    updateBuffer(
        short bufferIndex
    ) override {
        if (m_bufferVersions[bufferIndex] < m_touchedVersion) {
            m_buffers[bufferIndex] = this->latest();
            m_bufferVersions[bufferIndex] = m_touchedVersion;
        }
    }


    Data&
    workingCopy() {
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

    FrameIndex m_touchedVersion = 0;

    std::array<Data, 3> m_buffers;

    std::array<FrameIndex, 3> m_bufferVersions = {{0, 0, 0}};
};


template<
    typename Data, 
    ThreadId Writer, ThreadId Reader
>
class SharedQueue {

public:

    using State = SharedState<Writer, Reader>;

    typename std::deque<Data>::const_iterator
    begin() {
        return this->entries().cbegin();
    }

    typename std::deque<Data>::const_iterator
    end() {
        return this->entries().cend();
    }

    void
    push(
        Data data
    ) {
        FrameIndex frameIndex = State::instance().getBufferVersion(StateBuffer::WorkingCopy);
        boost::lock_guard<boost::mutex> lock(m_mutex);
        m_workingQueue.emplace_back(frameIndex, std::forward<Data>(data));
        assert(m_workingQueue.size() < 1000 && "Queue is pretty full. Is there a consumer?");
    }

    const std::deque<Data>&
    entries() {
        this->updateStableQueue();
        return m_stableQueue;
    }

private:

    void updateStableQueue() {
        FrameIndex stableFrame = State::instance().stableFrame();
        if (m_lastStableUpdate == stableFrame) {
            return;
        }
        m_lastStableUpdate = stableFrame;
        FrameIndex stableVersion = State::instance().getBufferVersion(StateBuffer::Stable);
        m_stableQueue.clear();
        boost::lock_guard<boost::mutex> lock(m_mutex);
        while (
            not m_workingQueue.empty() 
            and m_workingQueue.front().first <= stableVersion
        ) {
            m_stableQueue.emplace_back(
                std::move(m_workingQueue.front().second)
            );
            m_workingQueue.pop_front();
        }
    }

    FrameIndex m_lastStableUpdate = 0;

    boost::mutex m_mutex;

    std::deque<std::pair<FrameIndex, Data>> m_workingQueue;

    std::deque<Data> m_stableQueue;
};


////////////////////////////////////////////////////////////////////////////////
// Render State (Script => Render)
////////////////////////////////////////////////////////////////////////////////
extern template class SharedState<ThreadId::Script, ThreadId::Render>;
using RenderState = SharedState<ThreadId::Script, ThreadId::Render>;

template<typename Data, bool updateWorkingCopy=true>
using RenderData = SharedData<Data, ThreadId::Script, ThreadId::Render, updateWorkingCopy>;

template<typename Data>
using RenderQueue = SharedQueue<Data, ThreadId::Script, ThreadId::Render>;

////////////////////////////////////////////////////////////////////////////////
// Input State (Render => Script)
////////////////////////////////////////////////////////////////////////////////
extern template class SharedState<ThreadId::Render, ThreadId::Script>;
using InputState = SharedState<ThreadId::Render, ThreadId::Script>;

template<typename Data, bool updateWorkingCopy=true>
using InputData = SharedData<Data, ThreadId::Render, ThreadId::Script, updateWorkingCopy>;

template<typename Data>
using InputQueue = SharedQueue<Data, ThreadId::Render, ThreadId::Script>;


}
