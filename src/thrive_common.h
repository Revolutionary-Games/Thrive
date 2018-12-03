// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
#include <Addons/GameModule.h>

#include <memory>

namespace thrive {

//! \brief Common code shared between the client and the server
class ThriveCommon {
    struct Implementation;

public:
    ThriveCommon();
    ~ThriveCommon();

    Leviathan::GameModule*
        getMicrobeScripts();

    //! \brief This creates physics materials for a Thrive world
    std::unique_ptr<Leviathan::PhysicsMaterialManager>
        createPhysicsMaterials() const;

    static ThriveCommon*
        get();

protected:
    //! \brief Loads scripts and json config files
    bool
        loadScriptsAndConfigs();

    void
        releaseScripts();

    //! \brief Calls initialization methods for scripts that are common between
    //! client and server
    bool
        scriptSetup();

protected:
    std::unique_ptr<Implementation> m_commonImpl;

private:
    static ThriveCommon* staticInstance;
};

} // namespace thrive
