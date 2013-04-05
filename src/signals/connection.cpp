#include "signals/connection.h"

using namespace thrive::signals;

void
Connection::disconnect() {
    m_isConnected = false;
}


bool
Connection::isConnected() {
    return m_isConnected;
}
