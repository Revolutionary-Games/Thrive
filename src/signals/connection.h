#pragma once

#include <memory>

namespace thrive {

class Connection {
public:

    using Ptr = std::shared_ptr<Connection>;

    void
    disconnect();

    bool
    isConnected();

private:

    bool m_isConnected = true;
};

}
