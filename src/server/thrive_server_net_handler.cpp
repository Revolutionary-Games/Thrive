// ------------------------------------ //
#include "thrive_server_net_handler.h"

using namespace thrive;
// ------------------------------------ //
ThriveServerNetHandler::ThriveServerNetHandler() :
    NetworkServerInterface(10,
        "Thrive multiplayer prototype server",
        Leviathan::SERVER_JOIN_RESTRICT::None)
{}

ThriveServerNetHandler::~ThriveServerNetHandler() {}
// ------------------------------------ //
