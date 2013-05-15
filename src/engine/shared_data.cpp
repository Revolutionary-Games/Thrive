#include "engine/shared_data.h"

using namespace thrive;


template class SharedState<ThreadId::Script, ThreadId::Render>;
template class SharedState<ThreadId::Render, ThreadId::Script>;
template class SharedState<ThreadId::Physics, ThreadId::Script>;

