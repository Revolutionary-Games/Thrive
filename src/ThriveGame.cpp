// ------------------------------------ //
#include "ThriveGame.h"

#include "thrive_net_handler.h"

#include "Networking/NetworkHandler.h"
#include "Rendering/GraphicalInputEntity.h"

using namespace thrive;
// ------------------------------------ //
ThriveGame::ThriveGame(){

    StaticGame = this;
}

ThriveGame::~ThriveGame(){

    StaticGame = nullptr;
}

std::string ThriveGame::GenerateWindowTitle(){

    return "Fabricators Client";
}

ThriveGame* ThriveGame::Get(){

    return StaticGame;
}

ThriveGame* ThriveGame::StaticGame = nullptr;

Leviathan::NetworkInterface* ThriveGame::_GetApplicationPacketHandler(){

    if(!Network)
        Network = std::make_unique<ThriveNetHandler>();
    return Network.get();
}

void ThriveGame::_ShutdownApplicationPacketHandler(){

    Network.reset();
}
// ------------------------------------ //
void ThriveGame::Tick(int mspassed){

}

void ThriveGame::CustomizeEnginePostLoad(){

    // Load GUI documents (but only if graphics are enabled) //
    if(Engine::Get()->GetNoGui()){
        
        // Skip the graphical objects when not in graphical mode //
        return;
    }

    Leviathan::GraphicalInputEntity* window1 = Engine::GetEngine()->GetWindowEntity();

    Leviathan::Gui::GuiManager* GuiManagerAccess = window1->GetGui();

    if(!GuiManagerAccess->LoadGUIFile("./Data/Scripts/GUI/thrive_menus.txt")){
        
        LOG_ERROR("Thrive: failed to load the GuiFile, quitting");
        StartRelease();
    }

    window1->SetAutoClearing("");
    
}

void ThriveGame::EnginePreShutdown(){
        
}
// ------------------------------------ //
void ThriveGame::CheckGameConfigurationVariables(Lock &guard, GameConfiguration* configobj){
    
    
}

void ThriveGame::CheckGameKeyConfigVariables(Lock &guard, KeyConfiguration* keyconfigobj){

}
// ------------------------------------ //
bool ThriveGame::InitLoadCustomScriptTypes(asIScriptEngine* engine){

    if(engine->RegisterObjectType("ThriveGame", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0){
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("ThriveGame@ GetThriveGame()",
            asFUNCTION(ThriveGame::Get), asCALL_CDECL) < 0)
    {
        ANGELSCRIPT_REGISTERFAIL;
    }
    
    // if(engine->RegisterObjectMethod("Client",
    //         "bool Connect(const string &in address, string &out errormessage)",
    //         asMETHODPR(Client, Connect, (const std::string&, std::string&), bool),
    //         asCALL_THISCALL) < 0)
    // {
    //     ANGELSCRIPT_REGISTERFAIL;
    // }

    
    return true;
}

void ThriveGame::RegisterCustomScriptTypes(asIScriptEngine* engine,
    std::map<int, std::string> &typeids)
{
    typeids.insert(std::make_pair(engine->GetTypeIdByDecl("ThriveGame"), "ThriveGame"));
}



