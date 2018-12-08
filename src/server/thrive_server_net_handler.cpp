// ------------------------------------ //
#include "thrive_server_net_handler.h"

#include "ThriveServer.h"
#include "generated/cell_stage_world.h"
using namespace thrive;
// ------------------------------------ //
ThriveServerNetHandler::ThriveServerNetHandler() :
    NetworkServerInterface(10,
        "Thrive multiplayer prototype server",
        Leviathan::SERVER_JOIN_RESTRICT::None)
{}

ThriveServerNetHandler::~ThriveServerNetHandler() {}
// ------------------------------------ //
std::shared_ptr<GameWorld>
    ThriveServerNetHandler::_GetWorldForJoinTarget(const std::string& options)
{
    return ThriveServer::get()->getCellStageShared();
}
