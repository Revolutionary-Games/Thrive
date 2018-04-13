// ------------------------------------ //
#include "thrive_net_handler.h"

#include "GUI/GuiManager.h"

#include "Engine.h"
#include "Events/EventHandler.h"

using namespace thrive;
// ------------------------------------ //
ThriveNetHandler::ThriveNetHandler() : NetworkClientInterface()
{

}

ThriveNetHandler::~ThriveNetHandler(){

}
// ------------------------------------ //
void ThriveNetHandler::_OnStartApplicationConnect(){
    
	// Send our custom join request packet //

}
// ------------------------------------ //
void ThriveNetHandler::_OnNewConnectionStatusMessage(const std::string &message){
    
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("ConnectStatusMessage",
            Leviathan::NamedVars(std::shared_ptr<NamedVariableList>(
		new NamedVariableList("Message", new VariableBlock(message))))));
}
// ------------------------------------ //
void ThriveNetHandler::_OnDisconnectFromServer(const std::string &reasonstring,
    bool donebyus)
{
	// Enable the connection screen to display this message //
	//Engine::Get()->GetWindowEntity()->GetGUI()->SetCollectionState("ConnectionScreen", true);
}




