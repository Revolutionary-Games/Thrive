#pragma once

namespace thrive {

template<typename... Args>
class Event {

public:

    void
    operator ()(
        const Args&... args
    );

};


template<typename... Args>
class EventHandler {

public:

    std::deque<std::tuple<Args...>>
    getEvents(
        Args&... args
    ) {
        boost::lock_guard<boost::mutex> lock;
        std::deque<std::tuple<Args...>> result;
        auto iter = m_queue.begin();
        while (iter != m_queue.end()) {
            QueuedEvent& queuedEvent = *iter;
            FrameIndex currentFrame = 0;
            EngineRunner* emittingRunner = EngineRunner::get(
                queuedEvent.m_emittingThread
            );
            if (emittingRunner) {
                currentFrame = emittingRunner->engine().currentFrame();
            }
            if (queuedEvent.m_frame  currentFrame) {
                result.push_back(std::move(queuedEvent.m_args));

            }
            ++iter;
        }
        for (QueuedEvent& queuedEvent : m_queue) {
        }
    }

    void
    post(
        const Args&... args
    ) {
        FrameIndex index = 0;
        ThreadId threadId = ThreadId::Unknown;
        EngineRunner* currentRunner = EngineRunner::current();
        if (currentRunner) {
            index = currentRunner->engine().currentFrame();
        }
        boost::lock_guard<boost::mutex> lock;
        m_queue.emplace_back(index, args...);
    }

private:

    template<typename... Args>
    struct QueuedEvent {

        QueuedEvent(
            FrameIndex index,
            ThreadId emittingThread,
            const Args&... args
        ) : m_args(args...),
            m_emittingThread(emittingThread),
            m_frameIndex(index)
        {
        }

        QueuedEvent(QueuedEvent&& other) = default; 

        std::tuple<Args...> m_args; 

        ThreadId m_emittingThread;

        FrameIndex m_frameIndex;

    };

    boost::mutex m_mutex;

    std::deque<QueuedEvents> m_queue; 
};

}
