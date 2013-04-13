#pragma once

#include "engine/scripting.h"
#include "signals/connection.h"
#include "signals/guardian.h"
#include "util/scope_guard.h"

#include <assert.h>
#include <list>
#include <functional>
#include <memory>
#include <unordered_map>

namespace thrive {

class Connection;
class Guardian;
class PropertyBase;

class SignalBase {

public:

    virtual ~SignalBase() = 0;

    static SignalBase*
    getFromLua(
        lua_State* L,
        int index
    );

    int
    connectToLua(
        lua_State* L
    );

    void
    disconnectFromLua(
        lua_State* L,
        int slotReference
    );

    template<typename... Args>
    void
    emitToLua(Args&&... args) {
        this->removeStaleLuaSlots();
        for (auto pair : m_luaSlots) {
            lua_State* L = pair.first;
            int slotReference = pair.second;
            lua_rawgeti(L, LUA_REGISTRYINDEX, slotReference);
            LuaStack<Args...>::push(L, std::forward<Args>(args)...);
            lua_call(L, sizeof...(Args), 0);
        }
    }

    int
    pushToLua(
        lua_State* L
    );

private:

    void removeStaleLuaSlots();

    std::list< std::pair<lua_State*, int> > m_luaSlots;

    std::unordered_map<lua_State*, int> m_luaReferences;

    std::list< std::pair<lua_State*, int> > m_removedLuaSlots;

};

template<typename... Args>
class Signal : public SignalBase {

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
        this->emitToLua(arguments...);
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

        std::shared_ptr<Connection> m_connection;

        std::unique_ptr<Guardian> m_guardian;

    };

    bool m_emitting = false;

    std::list<Slot> m_slots;

    std::list<Slot> m_newSlots;
    

};

}
