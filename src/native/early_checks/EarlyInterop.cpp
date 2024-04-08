// ------------------------------------ //
#include "EarlyInterop.h"

#include "../helpers/CPUCheck.hpp"

// ------------------------------------ //
int32_t CheckEarlyAPIVersion()
{
    return EARLY_CHECK_LIBRARY_VERSION;
}

// ------------------------------------ //
CPU_CHECK_RESULT CheckRequiredCPUFeatures()
{
    return Thrive::CPUCheck::CheckCurrentCPU();
}

CPU_CHECK_RESULT CheckCompatibilityLibraryCPUFeatures()
{
    return Thrive::CPUCheck::CheckCurrentCPUCompatibilityMode();
}
