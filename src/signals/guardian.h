#pragma once

#include <memory>

namespace thrive {

class Guardian {
public:

    virtual ~Guardian() = 0;

    virtual bool
    isLocked() const = 0;

    virtual bool
    lock() = 0;

    virtual void
    unlock() = 0;
};


class GuardianLock {
public:

    GuardianLock(
        Guardian* guardian
    );

    GuardianLock(const GuardianLock&) = delete;

    ~GuardianLock();

    GuardianLock&
    operator= (const GuardianLock&) = delete;

    operator bool() const;

    void
    unlock();

private:

    Guardian* m_guardian;
};


template<typename T>
class SharedPtrGuardian : public Guardian {

public:

    using UPtr = std::unique_ptr<SharedPtrGuardian<T>>;

    SharedPtrGuardian(
        std::weak_ptr<T> weakPtr
    ) : m_weakPtr(weakPtr)
    {
    }

    bool
    isLocked() const override {
        return bool(m_sharedPtr);
    }

    bool
    lock() override {
        if (m_sharedPtr) {
            return true;
        }
        else {
            m_sharedPtr = m_weakPtr.lock();
            return bool(m_sharedPtr);
        }
    }

    void
    unlock() override {
        m_sharedPtr.reset();
    }

private:

    std::shared_ptr<T> m_sharedPtr;

    std::weak_ptr<T> m_weakPtr;

};

template<typename Ptr>
static typename SharedPtrGuardian<typename Ptr::element_type>::UPtr
createSharedPtrGuardian(
    Ptr ptr
) {
    using T = typename Ptr::element_type;
    return typename SharedPtrGuardian<T>::UPtr(new SharedPtrGuardian<T>(ptr));
}


}
