// Thrive Game
// Copyright (C) 2013-2017  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "Networking/NetworkClientInterface.h"
#include "Networking/NetworkInterface.h"

#include <string>

namespace thrive {

//! \brief Thrive specific NetworkClientInterface
class ThriveNetHandler : public Leviathan::NetworkClientInterface {
public:
    ThriveNetHandler();
    virtual ~ThriveNetHandler();

    //! \brief This is where we ask to join the cell stage on a server
    void
        _OnProperlyConnected() override;

protected:
    //! \brief Used to fire GenericEvents to update GUI status
    void
        _OnNewConnectionStatusMessage(const std::string& message) override;

    //! \brief This detects when the server kicks us and displays the reason
    void
        _OnDisconnectFromServer(const std::string& reasonstring,
            bool donebyus) override;

    std::shared_ptr<Leviathan::PhysicsMaterialManager>
        GetPhysicsMaterialsForReceivedWorld(int32_t worldtype,
            const std::string& extraoptions) override;

    void
        _OnWorldJoined(std::shared_ptr<GameWorld> world) override;
};
} // namespace thrive
