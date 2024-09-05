#pragma once

#include <godot_cpp/core/class_db.hpp>

namespace Thrive
{
void InitializeThriveModule(godot::ModuleInitializationLevel level);
void UnInitializeThriveModule(godot::ModuleInitializationLevel level);
} // namespace Thrive
