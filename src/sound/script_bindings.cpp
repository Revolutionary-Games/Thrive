#include "sound/script_bindings.h"

#include "scripting/luabind.h"
#include "sound/sound_source_system.h"

using namespace luabind;
using namespace thrive;

luabind::scope
thrive::SoundBindings::luaBindings() {
    return (
        Sound::luaBindings(),
        SoundSourceSystem::luaBindings(),
        SoundSourceComponent::luaBindings()
    );
}


