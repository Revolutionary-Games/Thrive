// Thrive Game
// Copyright (C) 2013-2017  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "Networking/NetworkInterface.h"
#include "Networking/NetworkClientInterface.h"

#include <string>

namespace thrive{

//! \brief Thrive specific NetworkClientInterface
class ThriveNetHandler : public Leviathan::NetworkClientInterface{
public:
    ThriveNetHandler();
    virtual ~ThriveNetHandler();

    //! \brief Joins the lobby or the match when the connection is confirmed
    void _OnStartApplicationConnect() override;

protected:

    //! \brief Used to fire GenericEvents to update GUI status
    void _OnNewConnectionStatusMessage(const std::string &message) override;

    //! \brief This detects when the server kicks us and displays the reason
    void _OnDisconnectFromServer(const std::string &reasonstring, bool donebyus) override;
};
}

