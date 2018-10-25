#include "microbe_stage/player_hover_info.h"

#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/player_microbe_control.h"
#include "microbe_stage/simulation_parameters.h"

#include "generated/cell_stage_world.h"

#include <Engine.h>
#include <Events/EventHandler.h>

#include <iomanip>

using namespace thrive;

void
    PlayerHoverInfoSystem::Run(CellStageWorld& world)
{
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

    // TODO: hovered microbes

    // TODO: perhaps this could detect any entity that has a Model component as
    // well (cells don't have one so that wouldn't work for them but for things
    // like floating organelles etc.)

    Engine::Get()->GetEventHandler()->CallEvent(event);
}
