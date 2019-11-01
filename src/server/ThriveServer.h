// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
//! \file \note This file needs to be named like it is currently
#include "thrive_common.h"
#include "thrive_server_net_handler.h"

#include "Application/ServerApplication.h"

namespace thrive {

class CellStageWorld;

//! This is the main thrive server class that is created in main.cpp and then
//! handles running the engine and the event loop
class ThriveServer : public Leviathan::ServerApplication, public ThriveCommon {
    class Implementation;

public:
    ThriveServer();
    virtual ~ThriveServer();

    // ------------------------------------ //
    // Gameplay etc. directly thrive related methods
    void
        setupServerWorlds();

    CellStageWorld*
        getCellStage();

    std::shared_ptr<CellStageWorld>
        getCellStageShared();


    void
        spawnPlayer(const std::shared_ptr<Leviathan::ConnectedPlayer>& player);


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

    static ThriveServer*
        get();

    bool
        InitLoadCustomScriptTypes(asIScriptEngine* engine) override;

private:
protected:
    Leviathan::NetworkInterface*
        _GetApplicationPacketHandler() override;
    void
        _ShutdownApplicationPacketHandler() override;

private:
    std::unique_ptr<ThriveServerNetHandler> m_network;

    // Some variables that have complex types are hidden here to not
    // have to include tons of headers here
    std::unique_ptr<Implementation> m_impl;

    static ThriveServer* staticInstance;
};

} // namespace thrive
// ------------------------------------ //
