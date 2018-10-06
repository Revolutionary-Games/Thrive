// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
//! \file \note This file needs to be named like it is currently

#include "Application/ClientApplication.h"
#include "Application/GameConfiguration.h"
#include "Application/KeyConfiguration.h"
#include "Events/EventHandler.h"
#include "GUI/GuiManager.h"

namespace thrive {

class CellStageWorld;

class ThriveNetHandler;

class PlayerData;

class PlayerMicrobeControl;

// class BioProcess;
// class Biome;

//! This is the main thrive class that is created in main.cpp and then handles
//! running the engine and the event loop
class ThriveGame : public Leviathan::ClientApplication {
    class Implementation;

public:
    ThriveGame();
    virtual ~ThriveGame();

    // ------------------------------------ //
    // Gameplay etc. directly thrive related methods
    void
        startNewGame();
    void
        loadSaveGame(const std::string& saveFile);
    void
        saveGame(const std::string& saveFile);

    CellStageWorld*
        getCellStage();

    PlayerData&
        playerData();

    PlayerMicrobeControl*
        getPlayerInput();

    Leviathan::GameModule*
        getMicrobeScripts();

    void
        setBackgroundMaterial(const std::string& material);

    // ------------------------------------ //
    // Player input actions
    void
        editorButtonClicked();

    void
        finishEditingClicked();

    void
        killPlayerCellClicked();

    void
        exitToMenuClicked();

    //! \param amount The amount the camera is moved. Positive moves away
    //! \todo Needs to detect the active camera system. Now always sends to the
    //! cell stage camera system
    void
        onZoomChange(float amount);

    // ------------------------------------ //
    // Hooking into the engine, and overridden methods from base application
    // etc.

    void
        Tick(int mspassed) override;

    void
        CustomizeEnginePostLoad() override;

    //! \brief This registers the physical materials (with callbacks for
    //! collision detection)
    void
        RegisterApplicationPhysicalMaterials(
            Leviathan::PhysicsMaterialManager* manager) override;

    void
        EnginePreShutdown() override;

    static std::string
        GenerateWindowTitle();

    // Game configuration checkers //
    static void
        CheckGameConfigurationVariables(Lock& guard,
            GameConfiguration* configobj);
    static void
        CheckGameKeyConfigVariables(Lock& guard,
            KeyConfiguration* keyconfigobj);

    static ThriveGame*
        Get();
    // Alternative for old engine style
    static ThriveGame*
        instance();

    bool
        InitLoadCustomScriptTypes(asIScriptEngine* engine) override;

private:
    //! \brief Calls initialization methods for scripts
    bool
        scriptSetup();

protected:
    Leviathan::NetworkInterface*
        _GetApplicationPacketHandler() override;
    void
        _ShutdownApplicationPacketHandler() override;

private:
    std::unique_ptr<ThriveNetHandler> Network;

    ObjectID m_cellCamera = 0;

    //! True once CustomizeEnginePostLoad has ran. This is used to
    //! delay methods that skip straight to the game in order to not
    //! start too soon
    bool m_postLoadRan = false;

    // Some variables that have complex types are hidden here to not
    // have to include tons of headers here
    std::unique_ptr<Implementation> m_impl;

    static ThriveGame* StaticGame;
};

} // namespace thrive
// ------------------------------------ //
