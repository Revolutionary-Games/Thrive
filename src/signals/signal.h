#pragma once

#include "signals/connection.h"
#include "signals/guardian.h"
#include "util/scope_guard.h"

#include <assert.h>
#include <list>
#include <functional>
#include <memory>

namespace thrive { namespace signals {

class Connection;
class Guardian;

template<typename... Args>
class Signal {

public:

    using Callback = std::function<void(Args...)>;

    ~Signal() {
        assert(not m_emitting and "Cannot destroy a signal from inside a slot.");
    }

    std::shared_ptr<Connection>
    connect(
        Callback callback,
        std::unique_ptr<Guardian> guardian = nullptr
    ) {
        auto connection = std::make_shared<Connection>();
        m_newSlots.emplace_front(
            callback,
            std::move(guardian),
            connection
        );
        return connection;
    }


    void
    operator() (Args... arguments) {
        assert(not m_emitting && "Cannot recursively emit signal.");
        ScopeGuard emissionFlagGuard(
            [this]() { m_emitting = true; },
            [this]() { m_emitting = false; }
        );
        m_slots.splice(m_slots.begin(), m_newSlots);
        auto iter = m_slots.begin();
        while (iter != m_slots.end()) {
            Slot& slot = *iter;
            GuardianLock lock(slot.m_guardian.get());
            if (lock and slot.m_connection->isConnected()) {
                slot.m_callback(arguments...);
                ++iter;
            }
            else {
                // Release the lock, because we will delete the guardian
                lock.unlock(); 
                iter = m_slots.erase(iter);
            }

        }
    }


private:

    struct Slot {

        Slot(
            Callback callback,
            std::unique_ptr<Guardian> guardian,
            Connection::Ptr connection
        ) : m_callback(callback),
            m_connection(connection),
            m_guardian(std::move(guardian))
        {
        }
        
        std::function<void(Args...)> m_callback;

        std::unique_ptr<Guardian> m_guardian;

        std::shared_ptr<Connection> m_connection;

    };

    bool m_emitting = false;

    std::list<Slot> m_slots;

    std::list<Slot> m_newSlots;
    

};

}}
