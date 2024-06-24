#pragma once

#ifdef __cplusplus
#include <cstdint>
#else
#include <stdint.h>
#endif

/// \brief Return codes for the CPU check
enum CPU_CHECK_RESULT : int32_t
{
    CPU_CHECK_SUCCESS = 0,
    CPU_CHECK_MISSING_AVX = 1,
    CPU_CHECK_MISSING_SSE41 = 2,
    CPU_CHECK_MISSING_SSE42 = 4,
    CPU_CHECK_MISSING_AVX2 = 8,
    CPU_CHECK_MISSING_LZCNT = 16,
    CPU_CHECK_MISSING_BMI1 = 32,
    CPU_CHECK_MISSING_FMA = 64,
    CPU_CHECK_MISSING_F16C = 128,
};
