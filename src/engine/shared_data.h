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

/**
 * @page shared_data Sharing Data across Threads
 *
 * To avoid lock contention, Thrive uses triple-buffering for 
 * synchronization of shared data. You can read up on triple-buffering
 * here:
 *  - http://blog.slapware.eu/game-engine/programming/multithreaded-renderloop-part1/
 *  - http://www.altdevblogaday.com/2011/07/03/threading-and-your-game-loop/
 *  - http://www.gamasutra.com/view/feature/1830/multithreaded_game_engine_.php
 *
 * As a quick recap, triple buffering uses three buffers to synchronize a 
 * writing thread and a reading thread:
 *  - Working Copy: This buffer is updated by the writing thread
 *  - Latest: This is the last buffer that was updated
 *  - Stable: This is the buffer the reading thread reads from
 * Only the working copy is writable. Stable and latest are read-only.
 *
 * There are two main classes handling shared state in Thrive: SharedState and
 * SharedData. SharedState handles the locking and release of the three 
 * buffers. SharedData is where the data is actually buffered.
 *
 * There is a distinction between \a state and \a data buffers. State buffers
 * are one of working copy, latest or stable. Data buffers are one of the
 * three that a SharedData object holds, indexed by 0, 1 or 2. The SharedState
 * handles the mapping between state and data buffers.
 *
 * SharedState keeps track of several frame counters. First, there's the 
 * working copy frame and the stable frame. Those are incremented after each
 * call to SharedState::releaseWorkingCopy() and SharedState::releaseStable(),
 * respectively. They effectively keep track of how many frames the writer /
 * reader threads have rendered each.
 *
 * For each data buffer, SharedState also keeps track of the last frame the
 * data buffer was the working copy. This is necessary for selecting the next
 * working copy (which should be the oldest data buffer that's not locked by
 * the reading thread).
 *
 * Inside a SharedData object, each data buffer has a version. The version is 
 * incremented each time SharedData::touch() is called, which should be done
 * whenever the data changes.
 *
 * @section using_shared_data Using SharedData
 *
 * If you need to share data across threads, you first have to identify which
 * thread is supposed to write the data and which one is supposed to read.
 * Triple-buffering only supports exactly one writer and one reader. If you 
 * need to synchronize more threads than that, you will either have to use
 * several SharedData instances with one central thread to synchronize them 
 * all or you will have to share the data over a different mechanism.
 *
 * There are some predefined SharedState and SharedData aliases at the bottom
 * of this header file. At the time of this writing, they are
 *  - RenderState / RenderData: Script thread writes, render thread reads
 *  - InputState / InputData: Render thread writes, script thread reads
 *
 * Once you have decided on reader and writer, you need to identify the
 * data you want to share. In the simplest case, it can be just an integer
 * or some other POD. You can also define your own data structure.
 *
 * After that, all you have to do is add a member of type SharedData to 
 * your class:
 * \code
 * class MyClass {
 *  public:
 *  
 *      struct Properties {
 *          int number;
 *          std::string text;
 *      };
 *
 *      RenderData<Properties>
 *      m_sharedProperties;
 * };
 * \endcode
 *
 * To access the shared properties, the reader thread has to call 
 * SharedData::stable(), which returns a const reference to the data in the
 * stable buffer:
 * \code
 * MyClass obj;
 * const MyClass::Properties& stable = obj.m_sharedProperties.stable();
 * \endcode
 *
 * The writer thread may access both the latest and the working copy buffer.
 * SharedData::workingCopy() returns a non-const reference so it can be 
 * changed. <b>Don't forget to call SharedData::touch() to let the system know
 * that you changed something!</b>
 * \code
 * MyClass obj;
 * const MyClass::Properties& latest = obj.m_sharedProperties.latest();
 * MyClass::Properties& workingCopy = obj.m_sharedProperties.workingCopy();
 * workingCopy.text = "New text";
 * obj.m_sharedProperties.touch();
 * \endcode
 *
 * @subsection touch() and untouch()
 *
 * When the writing thread has modified the data, it should call 
 * SharedData::touch() to mark the buffer as changed. The reading thread can
 * call SharedData::hasChanges() to query whether the current stable buffer
 * has any changes. Once it has processed these changes, it should call
 * SharedData::untouch() to mark them as such.
 *
 * @section shared_data_lua SharedData in Lua
 *
 * How you can access shared data from Lua depends on whether the script
 * thread is the reader or writer. If it's the reader, the class holding
 * the shared data should expose a \c stable property to Lua. So a C++
 * class like this:
 * \code
 * struct MyClass {
 *      struct Properties {
 *          int number;
 *          std::string string;
 *      };
 *
 *      SharedData<Properties, Thread::Render, Thread::Script> m_sharedData;
 * };
 * \endcode
 *
 * Should be accessible from Lua like this:
 * \code
 * obj = MyClass()
 * print(obj.stable.number)
 * print(obj.stable.string)
 * \endcode
 *
 * If the script thread is the writer, the class should expose three members:
 * - \c workingCopy: A writable reference to the working copy
 * - \c latest: A read-only reference to the latest buffer
 * - \c touch(): A function that calls SharedData::touch()
 * 
 */

/**
* @brief Enumeration naming the three state buffers
*/
enum class StateBuffer {
    Stable,     /**< Read-only buffer used by the reading thread */
    Latest,     /**< Read-only buffer that was last updated */
    WorkingCopy /**< Writable buffer that is currently begin updated by the writing thread */
};


namespace detail {
    template<ThreadId Writer, ThreadId Reader>
    struct SharedDataBase {
        virtual void updateBuffer(short bufferIndex) = 0;
    };
}

/**
* @brief Manages the state transitions
*
* SharedState works together with SharedData and keeps track of which data 
* buffer is stable, latest or working copy.
*
* Each SharedState has exactly one writing thread and one reading thread. If 
* you need more writers or readers, they will have to be synchronized by 
* other means.
*
* @tparam Writer
*   The reading thread
* @tparam Reader
*   The writing thread
*/
template<ThreadId Writer, ThreadId Reader>
class SharedState {

public:

    /**
    * @brief The singleton instance
    */
    static SharedState&
    instance() {
        static SharedState instance;
        return instance;
    }

    /**
    * @brief Returns the current data buffer index for the state buffer
    *
    * @param buffer
    *   The state buffer whose data buffer index you want to know
    *
    * @return 
    *   The data buffer index
    */
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

    /**
    * @brief The frame of the state buffer
    *
    * The buffer frame is incremented with each call to releaseWorkingCopy().
    *
    * @param buffer
    *   The state buffer whose frame you'd like to know
    *
    * @return 
    *   The buffer frame
    */
    FrameIndex
    getBufferFrame(
        StateBuffer buffer
    ) const {
        short index = this->getBufferIndex(buffer);
        assert(index >= 0 && "Invalid buffer specified. How did you manage that?!");
        return m_bufferFrames[index];
    }

    /**
    * @brief Locks the stable buffer for reading
    *
    * The new stable buffer will be the last fully updated data buffer.
    *
    * Calling this twice without a call to releaseStable() in between is an 
    * error.
    */
    void
    lockStable() {
        assert(m_stableBuffer == -1 && "Double locking stable buffer");
        m_stableBuffer = m_latestBuffer;
    }

    /**
    * @brief Locks the working copy for writing
    *
    * The new working copy will be the data buffer with the oldest frame.
    *
    * Calling this twice without a call to releaseWorkingCopy() in between is
    * an error.
    */
    void
    lockWorkingCopy() {
        assert(m_workingCopyBuffer == -1 && "Double locking working copy buffer");
        short oldestBuffer = (m_latestBuffer + 1) % 3;
        short secondOldestBuffer = (m_latestBuffer + 2) % 3;
        if (m_bufferFrames[secondOldestBuffer] < m_bufferFrames[oldestBuffer]) {
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

    /**
    * @brief Registers shared data with this state
    *
    * You usually don't have to call this yourself, SharedData does it
    * for you.
    *
    * @param sharedData
    *   The shared data to register
    */
    void
    registerSharedData(
        detail::SharedDataBase<Writer, Reader>* sharedData
    ) {
        m_registeredSharedData.insert(sharedData);
    }

    /**
    * @brief Releases the stable buffer
    *
    * The stable buffer becomes available for updating again.
    *
    * Also increments the stable frame index.
    */
    void
    releaseStable() {
        m_stableFrame += 1;
        m_stableBuffer = -1;
    }

    /**
    * @brief Releases the working copy
    *
    * The working copy becomes the latest buffer.
    *
    * Also increments the working copy frame.
    */
    void
    releaseWorkingCopy() {
        m_workingCopyFrame += 1;
        m_bufferFrames[m_workingCopyBuffer] = m_workingCopyFrame;
        m_latestBuffer = m_workingCopyBuffer;
        m_workingCopyBuffer = -1;
    }

    /**
    * @brief Resets this shared state
    *
    * This resets the frame indices and buffer indices to their initial
    * values. Only useful for testing.
    */
    void
    reset() {
        m_workingCopyFrame = 0;
        m_stableFrame = 0;
        m_latestBuffer = 0;
        m_stableBuffer = -1;
        m_workingCopyBuffer = -1;
        for (int i=0; i < 3; ++i) {
            m_bufferFrames[i] = 0;
        }
    }

    /**
    * @brief The current stable frame
    *
    * The stable frame is incremented for each call to releaseStable().
    *
    * @return 
    *   The current stable frame
    */
    FrameIndex
    stableFrame() const {
        return m_stableFrame;
    }

    /**
    * @brief The current working copy frame
    *
    * The frame is incremented for each call to releaseWorkingCopy().
    *
    * @return 
    *   The working copy frame
    */
    FrameIndex
    workingCopyFrame() const {
        return m_workingCopyFrame;
    }

    /**
    * @brief Unregisters shared data from this state
    *
    * You don't have to call this yourself, SharedData does it for you.
    *
    * @param sharedData
    *   The shared data to unregister
    */
    void
    unregisterSharedData(
        detail::SharedDataBase<Writer, Reader>* sharedData
    ) {
        m_registeredSharedData.erase(sharedData);
    }

private:

    std::array<FrameIndex, 3> m_bufferFrames = {{0, 0, 0}};

    short m_latestBuffer = 0;

    short m_stableBuffer = -1;

    FrameIndex m_stableFrame = 0;

    std::unordered_set<detail::SharedDataBase<Writer, Reader>*> m_registeredSharedData;

    short m_workingCopyBuffer = -1;

    FrameIndex m_workingCopyFrame = 0;

};


/**
* @brief A RAII helper for locking a data buffer
*
* The specializations of this template will lock the respective buffer
* on construction and release it on destruction.
*
* @tparam State
*   The SharedState to lock
* @tparam Buffer
*   The data buffer to lock
*/
template<
    typename State,
    StateBuffer Buffer
>
class StateLock { };


/**
* @brief Template specialization for locking the stable buffer
*
* @tparam State
*/
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

/**
* @brief Template specialization for locking the working copy buffer
*
* @tparam State
*/
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

/**
* @brief Triple buffers data for fast, thread-safe sharing
*
* @tparam Data_
*   The data structure to triple-buffer
*
* @tparam Writer
*   The writing thread
*
* @tparam Reader
*   The reading thread
*
* @tparam copyBuffers
*   Whether to overwrite a new working copy with the last frame. If your
*   writing thread only ever writes to the data structure and doesn't care 
*   about previous values, set this to \c false for improved performance.
*/
template<
    typename Data_, 
    ThreadId Writer, ThreadId Reader, 
    bool copyBuffers = true
>
class SharedData : public detail::SharedDataBase<Writer, Reader> {

public:

    /**
    * @brief Typedef for the triple-buffered data structure
    */
    using Data = Data_;

    /**
    * @brief Typedef for the SharedState this data belongs to
    */
    using State = SharedState<Writer, Reader>;

    /**
    * @brief Constructor
    *
    * This initializes all three buffers according to the passed
    * arguments. Since we are initializing three objects, there's
    * no move constructor available.
    *
    * @tparam Args
    *   Constructor signature of the data
    * @param args
    *   Constructor arguments for the buffered data
    *   
    */
    template<typename... Args>
    SharedData(
        const Args&... args
    ) : m_buffers{{Data{args...}, Data{args...}, Data{args...}}}
    {
        State::instance().registerSharedData(this);
    }

    /**
    * @brief Non-copiable
    *
    */
    SharedData(const SharedData&) = delete;

    /**
    * @brief Destructor
    */
    ~SharedData() {
        State::instance().unregisterSharedData(this);
    }

    /**
    * @brief Non-copy-assignable
    *
    */
    SharedData& operator= (const SharedData&) = delete;

    /**
    * @brief Whether the stable buffer is outdated
    *
    * @return 
    *   \c true if there have been changes to the current stable buffer since 
    *   the last call to untouch(), \c false otherwise
    */
    bool
    hasChanges() const {
        return m_lastUntouch < this->getLastBufferChange(StateBuffer::Stable);
    }

    /**
    * @brief Returns the latest data buffer
    */
    const Data&
    latest() const {
        return this->getBuffer(StateBuffer::Latest);
    }

    /**
    * @brief Returns the stable data buffer
    */
    const Data&
    stable() const {
        return this->getBuffer(StateBuffer::Stable);
    }

    /**
    * @brief Marks the working copy as changed
    *
    * Only the writing thread should call this
    */
    void
    touch() {
        State& state = State::instance();
        // +1 because we are currently rendering the *next* frame
        m_lastTouch = state.getBufferFrame(StateBuffer::WorkingCopy) + 1;
        this->setLastBufferChange(StateBuffer::WorkingCopy, m_lastTouch);
    }

    /**
    * @brief Resets the hasChanges() flag
    *
    * Only the reading thread should call this.
    */
    void
    untouch() {
        m_lastUntouch = this->getLastBufferChange(StateBuffer::Stable);
    }

    /**
    * @brief Updates a data buffer from the latest data
    *
    * @param bufferIndex
    *   The data buffer index to update
    */
    void
    updateBuffer(
        short bufferIndex
    ) override {
        if (m_lastBufferChanges[bufferIndex] < m_lastTouch) {
            m_buffers[bufferIndex] = this->latest();
            m_lastBufferChanges[bufferIndex] = m_lastTouch;
        }
    }

    /**
    * @brief Returns the working copy data buffer
    */
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

    FrameIndex
    getLastBufferChange(
        StateBuffer buffer
    ) const {
        State& state = State::instance();
        short bufferIndex = state.getBufferIndex(buffer);
        return m_lastBufferChanges[bufferIndex];
    }

    void
    setLastBufferChange(
        StateBuffer buffer,
        FrameIndex lastChange
    ) {
        State& state = State::instance();
        short bufferIndex = state.getBufferIndex(buffer);
        m_lastBufferChanges[bufferIndex] = lastChange;
    }

    std::array<Data, 3> m_buffers;

    std::array<FrameIndex, 3> m_lastBufferChanges = {{0, 0, 0}};

    FrameIndex m_lastTouch = 0;

    FrameIndex m_lastUntouch = 0;

};


/**
* @brief Provides a thread-safe queue
*
* With SharedQueue, you can safely share anything that SharedData is not
* suited for, e.g. events. Both the reader and the writer have separate
* queues. The writer thread appends to its own queue. Each time the 
* reader thread calls entries(), the reader-side queue flushes the
* write-side entries to the reader side. Previous entries on the
* reader side are discarded.
*
* It is safe to call entries() multiple times per frame, the update
* only occurs at most once per frame.
*
* @tparam Data
*   The data structure to queue
* @tparam Writer
*   The writing thread
* @tparam Reader
*   The reading thread
*/
template<
    typename Data, 
    ThreadId Writer, ThreadId Reader
>
class SharedQueue {

public:

    /**
    * @brief The SharedState this queue belongs to
    */
    using State = SharedState<Writer, Reader>;

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * entries().cbegin();
    * \endcode
    */
    typename std::deque<Data>::const_iterator
    begin() {
        return this->entries().cbegin();
    }

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * entries().cend();
    * \endcode
    */
    typename std::deque<Data>::const_iterator
    end() {
        return this->entries().cend();
    }

    /**
    * @brief Pushes data to the end of the queue
    *
    * Only the writer thread may call this.
    *
    * @param data
    *   The data to push
    */
    void
    push(
        Data data
    ) {
        FrameIndex frameIndex = State::instance().getBufferFrame(StateBuffer::WorkingCopy);
        boost::lock_guard<boost::mutex> lock(m_mutex);
        m_workingQueue.emplace_back(frameIndex, std::forward<Data>(data));
        assert(m_workingQueue.size() < 1000 && "Queue is pretty full. Is there a consumer?");
    }

    /**
    * @brief Updates the reader-side queue and returns it
    */
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
        FrameIndex stableBufferFrame = State::instance().getBufferFrame(StateBuffer::Stable);
        m_stableQueue.clear();
        boost::lock_guard<boost::mutex> lock(m_mutex);
        while (
            not m_workingQueue.empty() 
            and m_workingQueue.front().first <= stableBufferFrame
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
