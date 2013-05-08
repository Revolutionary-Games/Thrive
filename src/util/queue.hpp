#pragma once

#include <boost/thread/mutex.hpp>

namespace thrive {

template<typename T>
class Queue {

public:

    template<typename... Args>
    void
    emplace(
        Args&&... args
    ) {
        boost::lock_guard<boost::mutex> lock(m_mutex);
        m_queue.emplace(std::forward<Args>(args)...);
    }

    bool
    empty() const {
        boost::lock_guard<boost::mutex> lock(m_mutex);
        return m_queue.empty();
    }

    T
    pop() {
        boost::lock_guard<boost::mutex> lock(m_mutex);
        T value = std::move(m_queue.front());
        m_queue.pop();
        return value;
    }

    void
    push(
        T&& value
    ) {
        boost::lock_guard<boost::mutex> lock(m_mutex);
        m_queue.push(std::forward(value));
    }

private:

    boost::mutex m_mutex;

    std::queue<T> m_queue;
};


}
