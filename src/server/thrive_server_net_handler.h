// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "Networking/NetworkInterface.h"
#include "Networking/NetworkServerInterface.h"

#include <string>

namespace thrive {

//! \brief Thrive server specific NetworkServerInterface
class ThriveServerNetHandler : public Leviathan::NetworkServerInterface {
public:
    ThriveServerNetHandler();
    virtual ~ThriveServerNetHandler();

protected:
    std::shared_ptr<GameWorld>
        _GetWorldForJoinTarget(const std::string& options) override;
};
} // namespace thrive
