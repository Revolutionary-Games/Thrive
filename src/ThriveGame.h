// Thrive Game
// Copyright (C) 2013-2019  Revolutionary Games
#pragma once
// ------------------------------------ //
//! \file \note This file needs to be named like it is currently
#include "thrive_common.h"

#include "Application/ClientApplication.h"
#include "Application/GameConfiguration.h"
#include "Application/KeyConfiguration.h"
#include "Events/EventHandler.h"
#include "GUI/GuiManager.h"

namespace thrive {

namespace test {
class TestThriveGame;
}

class CellStageWorld;

class ThriveNetHandler;

class PlayerData;

class PlayerMicrobeControl;

class AutoEvo;
class Species;

//! This is the main thrive class that is created in main.cpp and then handles
//! running the engine and the event loop
class ThriveGame : public Leviathan::ClientApplication, public ThriveCommon {
    class Implementation;
    friend test::TestThriveGame;

public:
    ThriveGame();

    //! \brief Version for tests with incomplete engine instance
    ThriveGame(Leviathan::Engine* engine);

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

    AutoEvo&
        autoEvo();

    void
        setBackgroundMaterial(const std::string& material);

    //! \brief Sets the skybox by asset on the world and the light intensity of
    //! indirect light coming from the skybox
    void
        setSkybox(const std::string& assetName, float lightIntensity);

    //!\brief This is a callback for the camera controller to notify us of the
    //! new needed position of the background
    void
        notifyCameraDistance(float height);

    inline bool
        areCheatsEnabled() const
    {
        return m_cheatsEnabled;
    }

    //! \brief Called from scripts to report auto-evo external population
    //! changes \param species Target species. Assumes reference counted is
    //! incremented
    void
        addExternalPopulationEffect(Species* species,
            int32_t change,
            const std::string& reason);

    //! \brief Checks should auto-evo start
    void
        checkAutoEvoStart();

    //! \brief Updates loading screen
    void
        updateLoadingScreen(bool enabled,
            const std::string& status,
            const std::string& message);

    // ------------------------------------ //
    // Player input actions
    void
        enterPlanetEditor();

    void
        editPlanet(const std::string& editType, double value);

    void
        editorButtonClicked();

    void
        enableFreebuild();

    void
        finishEditingClicked();

    void
        killPlayerCellClicked();

    void
        exitToMenuClicked();

    void
        pause(bool pause);

    //! \param amount The amount the camera is moved. Positive moves away
    //! \todo Needs to detect the active camera system. Now always sends to the
    //! cell stage camera system
    void
        onZoomChange(float amount);

    void
        toggleDebugOverlay();

    void
        toggleDebugPhysics();

    //! \brief Moves the player to play in the specified patch
    //!
    //! Also does cleanup like despawning old compounds and cells
    void
        playerMovedToPatch(int32_t patchId);

    //! \returns The current patch
    int32_t
        getCurrentPatch() const;

    // ------------------------------------ //
    //! \brief Begins connecting to server at url
    void
        connectToServer(const std::string& url);

    //! \brief Disconnects from current server
    void
        disconnectFromServer(bool userInitiated,
            const std::string& reason = "Disconnect by user");

    //! \brief Called when we receive an entity from the server that is probably
    //! a cell
    //!
    //! This handles adding MicrobeComponent etc. to make the thing show up
    //! properly
    void
        doSpawnCellFromServerReceivedComponents(ObjectID id);

    //! \brief Called from the net handler when we have joined a world
    void
        reportJoinedServerWorld(std::shared_ptr<GameWorld> world);

    //! \brief Called when the entity we control changes
    void
        reportLocalControlChanged(GameWorld* world);

    // ------------------------------------ //
    // Hooking into the engine, and overridden methods from base application
    // etc.

    void
        Tick(float elapsed) override;

    void
        CustomizeEnginePostLoad() override;

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

    //! \deprecated
    static ThriveGame*
        Get();

    //! This variant should be preferred as this follows thrive naming
    //! convention
    static ThriveGame*
        get();

    // Alternative for old engine style
    static ThriveGame*
        instance();

    bool
        InitLoadCustomScriptTypes(asIScriptEngine* engine) override;

protected:
    Leviathan::NetworkInterface*
        _GetApplicationPacketHandler() override;
    void
        _ShutdownApplicationPacketHandler() override;

    bool
        createImpl();

    void
        _checkIsEditorEntryReady();

private:
    std::unique_ptr<ThriveNetHandler> m_network;

    ObjectID m_cellCamera = 0;

    //! True once CustomizeEnginePostLoad has ran. This is used to
    //! delay methods that skip straight to the game in order to not
    //! start too soon
    bool m_postLoadRan = false;

    //! Controls if cheat keys are enabled.
    //! These are enabled by default when not making releases
    bool m_cheatsEnabled = false;

    //! True when debug overlay is enabled and updates should be sent to it
    bool m_debugOverlayEnabled = false;

    bool m_physicsDebugEnabled = false;

    // Some variables that have complex types are hidden here to not
    // have to include tons of headers here
    std::unique_ptr<Implementation> m_impl;

    static ThriveGame* StaticGame;
};

} // namespace thrive
// ------------------------------------ //
