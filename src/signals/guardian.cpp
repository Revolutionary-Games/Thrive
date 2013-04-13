#include "signals/guardian.h"

using namespace thrive;


Guardian::~Guardian() {}


GuardianLock::GuardianLock(
    Guardian* guardian
) : m_guardian(guardian)
{
    if (guardian) {
        guardian->lock();
    }
}


GuardianLock::~GuardianLock() {
    this->unlock();
}


GuardianLock::operator bool() const {
    if (m_guardian) {
        return m_guardian->isLocked();
    }
    else {
        return true;
    }
}


void
GuardianLock::unlock() {
    if (m_guardian) {
        m_guardian->unlock();
    }
    m_guardian = nullptr;
}
