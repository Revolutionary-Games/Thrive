// Thrive Game
// Copyright (C) 2013-2017  Revolutionary Games
#pragma once
// ------------------------------------ //
//! \file \note This file needs to be named like it is currently

#include "GUI/GuiManager.h"
#include "Application/GameConfiguration.h"
#include "Application/KeyConfiguration.h"
#include "Application/ClientApplication.h"
#include "Events/EventHandler.h"
#include "Script/ScriptExecutor.h"

namespace thrive{

class CellStageWorld;

class ThriveNetHandler;

class PlayerData;

//class BioProcess;
//class Biome;

//! This is the main thrive class that is created in main.cpp and then handles running
//! the engine and the event loop
class ThriveGame : public Leviathan::ClientApplication{
    class Implementation;
public:

    ThriveGame();
    virtual ~ThriveGame();

    // ------------------------------------ //
    // Gameplay etc. directly thrive related methods
    void startNewGame();

    //! \brief Creates a new cell of the current player species
    void respawnPlayerCell();


    CellStageWorld* getCellStage();

    PlayerData&
    playerData();

    // ------------------------------------ //
    // Hooking into the engine, and overridden methods from base application etc.

    void Tick(int mspassed) override;

    void CustomizeEnginePostLoad() override;
    void EnginePreShutdown() override;

    static std::string GenerateWindowTitle();

    // Game configuration checkers //
    static void CheckGameConfigurationVariables(Lock &guard, GameConfiguration* configobj);
    static void CheckGameKeyConfigVariables(Lock &guard, KeyConfiguration* keyconfigobj);

    static ThriveGame* Get();

    bool InitLoadCustomScriptTypes(asIScriptEngine* engine) override;
    void RegisterCustomScriptTypes(asIScriptEngine* engine,
        std::map<int, std::string> &typeids) override;
        
protected:

    Leviathan::NetworkInterface* _GetApplicationPacketHandler() override;
    void _ShutdownApplicationPacketHandler() override;
    
private:
    
    std::unique_ptr<ThriveNetHandler> Network;

    std::shared_ptr<CellStageWorld> m_cellStage;

    ObjectID m_cellCamera = 0;

    ObjectID m_backgroundPlane = 0;

    //! Player's cell
    ObjectID m_playerCell = 0;

    

    // TODO: remove this and the debug stuff in Tick
    int dummyTestCounter = 0;

    // Some variables that have complex types are hidden here to not
    // have to include tons of headers here
    std::unique_ptr<Implementation> m_impl;

    static ThriveGame* StaticGame;
};

}
// ------------------------------------ //


