// ------------------------------------ //
#include "thrive_net_handler.h"

#include "ThriveGame.h"

#include <Engine.h>
#include <Events/EventHandler.h>
#include <GUI/GuiManager.h>
#include <Physics/PhysicsMaterialManager.h>

using namespace thrive;
// ------------------------------------ //
ThriveNetHandler::ThriveNetHandler() : NetworkClientInterface() {}

ThriveNetHandler::~ThriveNetHandler() {}
// ------------------------------------ //
void
    ThriveNetHandler::_OnProperlyConnected()
{
    DoJoinDefaultWorld();
}
// ------------------------------------ //
void
    ThriveNetHandler::_OnNewConnectionStatusMessage(const std::string& message)
{
    Leviathan::GenericEvent::pointer event =
        new Leviathan::GenericEvent("ConnectStatusMessage");

    auto vars = event->GetVariables();

    vars->Add(std::make_shared<NamedVariableList>(
        "show", new Leviathan::BoolBlock(true)));
    vars->Add(std::make_shared<NamedVariableList>(
        "message", new Leviathan::StringBlock(message)));

    Engine::Get()->GetEventHandler()->CallEvent(event.detach());
}
// ------------------------------------ //
void
    ThriveNetHandler::_OnDisconnectFromServer(const std::string& reasonstring,
        bool donebyus)
{}
// ------------------------------------ //
std::shared_ptr<Leviathan::PhysicsMaterialManager>
    ThriveNetHandler::GetPhysicsMaterialsForReceivedWorld(int32_t worldtype,
        const std::string& extraoptions)
{
    return ThriveGame::Get()->createPhysicsMaterials();
}

void
    ThriveNetHandler::_OnWorldJoined(std::shared_ptr<GameWorld> world)
{
    ThriveGame::Get()->reportJoinedServerWorld(world);
}

void
    ThriveNetHandler::_OnLocalControlChanged(GameWorld* world)
{
    ThriveGame::Get()->reportLocalControlChanged(world);
}

void
    ThriveNetHandler::_OnEntityReceived(GameWorld* world, ObjectID created)
{
    ThriveGame::Get()->doSpawnCellFromServerReceivedComponents(created);
}
