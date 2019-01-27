#include "microbe_stage/player_hover_info.h"

#include "ThriveGame.h"
#include "engine/player_data.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/membrane_system.h"
#include "microbe_stage/player_microbe_control.h"
#include "microbe_stage/simulation_parameters.h"

#include "generated/cell_stage_world.h"

#include <Engine.h>
#include <Entities/ScriptComponentHolder.h>
#include <Events/EventHandler.h>
#include <Script/ScriptTypeResolver.h>

#include <iomanip>

using namespace thrive;

void
    PlayerHoverInfoSystem::Run(CellStageWorld& world)
{
    // Only on client
    if(!ThriveGame::Get())
        return;

    passed += Leviathan::TICKSPEED;

    if(passed < RUN_EVERY_MS)
        return;

    passed -= RUN_EVERY_MS;

    Float3 lookPoint;

    try {
        lookPoint = PlayerMicrobeControlSystem::getTargetPoint(world);
    } catch(const Leviathan::InvalidState& e) {
        // We just ignore this as we don't check if the player has spawned as
        // this could realistically work even while just observing (if that's
        // ever implemented)
        return;
    }

    // We use this to avoid leaking if something throws before we call the event
    auto event = Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
        "PlayerMouseHover");

    auto vars = event->GetVariables();

    std::stringstream posStr;
    posStr << std::fixed << std::setprecision(1) << lookPoint;

    vars->Add(std::make_shared<NamedVariableList>(
        "mousePos", new Leviathan::StringBlock(posStr.str())));

    // Detect compounds
    const auto compounds =
        world.GetCompoundCloudSystem().getAllAvailableAt(lookPoint);

    if(compounds.empty()) {

        vars->Add(std::make_shared<NamedVariableList>(
            "noCompounds", new Leviathan::BoolBlock(true)));

    } else {
        for(const auto& tuple : compounds) {

            std::stringstream compoundInfo;
            compoundInfo << std::fixed << std::setprecision(2)
                         << SimulationParameters::compoundRegistry
                                .getTypeData(std::get<0>(tuple))
                                .displayName
                         << ": " << std::get<1>(tuple);

            vars->Add(std::make_shared<NamedVariableList>(
                "compound" + std::to_string(std::get<0>(tuple)),
                new Leviathan::StringBlock(compoundInfo.str())));
        }
    }

    // Hovered over cells
    auto hovered = std::make_shared<NamedVariableList>("hoveredCells");

    auto microbeComponents = world.GetScriptComponentHolder("MicrobeComponent");

    // Only run when scripts are loaded
    // TODO: move this to a sub function to not have this huge indended block
    // here
    if(microbeComponents) {

        // The world will keep this alive (this is released immediately to
        // reduce chance of leaks)
        microbeComponents->Release();

        const auto stringType =
            Leviathan::AngelScriptTypeIDResolver<std::string>::Get(
                Leviathan::GetCurrentGlobalScriptExecutor());

        // This is used to skip the player
        auto controlledEntity =
            ThriveGame::Get()->playerData().activeCreature();

        const auto& allSpecies = world.GetComponentIndex_SpeciesComponent();

        auto& index = CachedComponents.GetIndex();
        for(auto iter = index.begin(); iter != index.end(); ++iter) {

            const float distance =
                (std::get<1>(*iter->second).Members._Position - lookPoint)
                    .Length();

            // Find only cells that have the mouse position within their
            // membrane
            if(distance >
                std::get<0>(*iter->second).calculateEncompassingCircleRadius())
                continue;

            // Skip player
            if(iter->first == controlledEntity)
                continue;

            // Hovered over this. Find the name of the species
            auto microbeComponent = microbeComponents->Find(iter->first);

            if(!microbeComponent)
                continue;

            // We don't store the reference to the object. The holder will keep
            // the reference alive while we work on it
            microbeComponent->Release();

            if(microbeComponent->GetPropertyCount() < 1) {

                LOG_ERROR("PlayerHoverInfoSystem: Run: MicrobeComponent object "
                          "has no properties");
                continue;
            }

            if(microbeComponent->GetPropertyTypeId(0) != stringType ||
                std::strncmp(microbeComponent->GetPropertyName(0),
                    "speciesName", sizeof("speciesName") - 1) != 0) {

                LOG_ERROR("PlayerHoverInfoSystem: Run: MicrobeComponent object "
                          "doesn't "
                          "have \"string speciesName\" as the first property");
                continue;
            }

            const auto* name = static_cast<std::string*>(
                microbeComponent->GetAddressOfProperty(0));

            bool found = false;

            for(const auto& tuple : allSpecies) {

                SpeciesComponent* species = std::get<1>(tuple);

                if(species->name == *name) {

                    hovered->PushValue(std::make_unique<VariableBlock>(
                        new Leviathan::StringBlock(
                            species->genus + " " + species->epithet)));
                    found = true;
                    break;
                }
            }

            if(!found) {

                // If we can't find the species, assume that it is extinct
                hovered->PushValue(std::make_unique<VariableBlock>(
                    new Leviathan::StringBlock("Extinct(" + *name + ")")));
            }
        }
    }

    vars->Add(hovered);

    // TODO: perhaps this could detect any entity that has a Model component as
    // well (cells don't have one so that wouldn't work for them but for things
    // like floating organelles etc.)

    Engine::Get()->GetEventHandler()->CallEvent(event);
}
