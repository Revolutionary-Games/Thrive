//! Tests that all the scripts compile
#include "scripting/script_initializer.h"

#include "Addons/GameModule.h"
#include "Addons/GameModuleLoader.h"
#include "FileSystem.h"
#include "Handlers/IDFactory.h"
#include "Script/ScriptExecutor.h"
#include "Script/ScriptModule.h"

#include "LeviathanTest/PartialEngine.h"

#include "catch.hpp"


TEST_CASE("Microbe scripts compile", "[scripts]")
{
    Leviathan::Test::PartialEngine<false> engine;

    Leviathan::IDFactory ids;
    Leviathan::ScriptExecutor exec;

    REQUIRE(thrive::registerThriveScriptTypes(exec.GetASEngine()));

    // Filesystem required for search //
    Leviathan::FileSystem filesystem;
    REQUIRE(filesystem.Init(&engine.Log));
    Leviathan::GameModuleLoader loader;
    loader.Init();


    Leviathan::GameModule::pointer module;
    REQUIRE_NOTHROW(module = loader.Load("microbe_stage", "ThriveGame"));

    // This is an extra check (currently, because the module loader should
    // always throw, but here in thrive code it is a good idea to test this to
    // not have the engine behaviour change)
    REQUIRE(module);
}
