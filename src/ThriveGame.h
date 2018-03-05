// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
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


    CellStageWorld* getCellStage();

    PlayerData&
    playerData();

    // ------------------------------------ //
    // Player input actions
    void onIntroSkipPressed();

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
    // Alternative for old engine style
    static ThriveGame* instance();

    bool InitLoadCustomScriptTypes(asIScriptEngine* engine) override;
private:

    //! \brief Calls initialization methods for scripts
    bool scriptSetup();

    bool _runCellStageSetupFunc(const std::string &name);
    
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

    //! True once CustomizeEnginePostLoad has ran. This is used to
    //! delay methods that skip straight to the game in order to not
    //! start too soon
    bool m_postLoadRan = false;

    // Some variables that have complex types are hidden here to not
    // have to include tons of headers here
    std::unique_ptr<Implementation> m_impl;

    static ThriveGame* StaticGame;
};

}
// ------------------------------------ //


