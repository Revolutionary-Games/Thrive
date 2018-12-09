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

GameWorld*
    ThriveServerNetHandler::_GetWorldForEntityMessage(int32_t worldid)
{
    auto world = ThriveServer::get()->getCellStage();

    if(world->GetID() != worldid) {

        LOG_ERROR(
            "ThriveServerNetHandler: got request for non-cellstage world id: " +
            std::to_string(worldid));
        return nullptr;
    }

    return world;
}

void
    ThriveServerNetHandler::_OnPlayerJoinedWorld(
        const std::shared_ptr<Leviathan::ConnectedPlayer>& player,
        const std::shared_ptr<GameWorld>& world)
{
    return ThriveServer::get()->spawnPlayer(player);
}
