#pragma once

#include "CPUCheckResult.h"

// CPU feature checking. Code approach is combined from the various answers here:
// https://stackoverflow.com/questions/6121792/how-to-check-if-a-cpu-supports-the-sse3-instruction-set

#ifdef _WIN32
#ifdef _MSC_VER
#include <intrin.h>

#define cpuid(info, x) __cpuidex(info, x, 0)
#else
// MinGW
#include <intrin.h>

inline void cpuid(int info[4], unsigned int infoType)
{
    __cpuid(info, infoType);
}
#endif

#elif defined(__aarch64__) || defined(__arm__)
#include <cstdlib>
#include <iostream>

#pragma message("Early check is not implemented for ARM!")

inline void cpuid(int info[4], unsigned int infoType)
{
    UNUSED(infoType);
    UNUSED(info);
    std::cout << "Early check is not implemented for ARM" << std::endl;
    std::abort();
}

// This is only for x86 so we need to provide a dummy one
inline int _xgetbv(int dummy) // NOLINT(*-reserved-identifier)
{
    UNUSED(dummy);
    std::cout << "Using dummy _xgetbv for ARM\n";
    return 0x6;
}

#else
#include <cpuid.h>
#include <immintrin.h>

inline void cpuid(int info[4], unsigned int infoType)
{
    __cpuid_count(infoType, 0, info[0], info[1], info[2], info[3]);
}

#endif

namespace Thrive
{
class CPUCheck
{
public:
    /// \brief Checks current CPU has all the needed features for Thrive library by default
    [[nodiscard]] static CPU_CHECK_RESULT CheckCurrentCPU() noexcept
    {
        ReadCPUFeatures();

        int32_t result = CheckCurrentCPUCompatibilityMode();

        // Result is built with bitwise operations to return all problems at once
        if (!avxSupported)
            result |= CPU_CHECK_MISSING_AVX;

        if (!avx2Supported)
            result |= CPU_CHECK_MISSING_AVX2;

        if (!bmi1Supported)
            result |= CPU_CHECK_MISSING_BMI1;

        if (!fma3Supported)
            result |= CPU_CHECK_MISSING_FMA;

        // Looking at the instructions, Jolt *probably* only uses FMA3. FMA4 is also way newer.
        /*if (!fma4Supported)
            result |= CPU_CHECK_MISSING_FMA;*/

        return static_cast<CPU_CHECK_RESULT>(result);
    }

    /// \brief Check variant that is for the max compatibility with older CPUs library version
    [[nodiscard]] static CPU_CHECK_RESULT CheckCurrentCPUCompatibilityMode() noexcept
    {
        int32_t result = CPU_CHECK_SUCCESS;

        if (!sse41Supported)
            result |= CPU_CHECK_MISSING_SSE41;

        if (!sse42Supported)
            result |= CPU_CHECK_MISSING_SSE42;

        return static_cast<CPU_CHECK_RESULT>(result);
    }

    [[nodiscard]] static bool HasAVX() noexcept
    {
        ReadCPUFeatures();
        return avxSupported && avx2Supported;
    }

    [[nodiscard]] static bool HasSSE41() noexcept
    {
        ReadCPUFeatures();
        return sse41Supported;
    }

    [[nodiscard]] static bool HasSSE42() noexcept
    {
        ReadCPUFeatures();
        return sse42Supported;
    }

private:
    static void ReadCPUFeatures()
    {
        if (featuresRead)
            return;

        featuresRead = true;

        // The following code is from StackOverflow answer: https://stackoverflow.com/a/7495023
        // Some parts are commented out as unneeded checks. And default values are added.
        // Misc.
        /*bool HW_MMX;
        bool HW_x64 = false;
        bool HW_ABM = false; // Advanced Bit Manipulation
        bool HW_RDRAND = false;*/
        bool HW_BMI1 = false;
        /*bool HW_BMI2 = false;
        bool HW_ADX = false;
        bool HW_PREFETCHWT1 = false;*/

        // SIMD: 128-bit
        bool HW_SSE = false;
        bool HW_SSE2 = false;
        bool HW_SSE3 = false;
        // bool HW_SSSE3 = false;
        bool HW_SSE41 = false;
        bool HW_SSE42 = false;
        bool HW_SSE4a = false;
        /*bool HW_AES = false;
        bool HW_SHA = false;*/

        // SIMD: 256-bit
        bool HW_AVX = false;
        /*bool HW_XOP = false;*/
        bool HW_FMA3 = false;
        bool HW_FMA4 = false;
        bool HW_AVX2 = false;

        // SIMD: 512-bit
        bool HW_AVX512F = false; // AVX512 Foundation
        // TODO: do these all specific things need checking
        /*bool HW_AVX512CD = false; // AVX512 Conflict Detection
        bool HW_AVX512PF = false; // AVX512 Prefetch
        bool HW_AVX512ER = false; // AVX512 Exponential + Reciprocal
        bool HW_AVX512VL = false; // AVX512 Vector Length Extensions
        bool HW_AVX512BW = false; // AVX512 Byte + Word
        bool HW_AVX512DQ = false; // AVX512 Doubleword + Quadword
        bool HW_AVX512IFMA = false; // AVX512 Integer 52-bit Fused Multiply-Add
        bool HW_AVX512VBMI = false; // AVX512 Vector Byte Manipulation Instructions*/

        int info[4];
        cpuid(info, 0);
        int nIds = info[0];

        cpuid(info, 0x80000000);
        unsigned nExIds = info[0];

        // Detect Features
        if (nIds >= 0x00000001)
        {
            cpuid(info, 0x00000001);
            // HW_MMX = (info[3] & ((int)1 << 23)) != 0;
            HW_SSE = (info[3] & ((int)1 << 25)) != 0;
            HW_SSE2 = (info[3] & ((int)1 << 26)) != 0;
            HW_SSE3 = (info[2] & ((int)1 << 0)) != 0;

            // HW_SSSE3 = (info[2] & ((int)1 << 9)) != 0;
            HW_SSE41 = (info[2] & ((int)1 << 19)) != 0;
            HW_SSE42 = (info[2] & ((int)1 << 20)) != 0;
            // HW_AES = (info[2] & ((int)1 << 25)) != 0;

            HW_AVX = (info[2] & ((int)1 << 28)) != 0;
            HW_FMA3 = (info[2] & ((int)1 << 12)) != 0;

            // HW_RDRAND = (info[2] & ((int)1 << 30)) != 0;
        }

        if (nIds >= 0x00000007)
        {
            cpuid(info, 0x00000007);
            HW_AVX2 = (info[1] & ((int)1 << 5)) != 0;

            HW_BMI1 = (info[1] & ((int)1 << 3)) != 0;
            // HW_BMI2 = (info[1] & ((int)1 << 8)) != 0;
            // HW_ADX = (info[1] & ((int)1 << 19)) != 0;
            // HW_SHA = (info[1] & ((int)1 << 29)) != 0;
            // HW_PREFETCHWT1 = (info[2] & ((int)1 << 0)) != 0;

            HW_AVX512F = (info[1] & ((int)1 << 16)) != 0;
            /*HW_AVX512CD = (info[1] & ((int)1 << 28)) != 0;
            HW_AVX512PF = (info[1] & ((int)1 << 26)) != 0;
            HW_AVX512ER = (info[1] & ((int)1 << 27)) != 0;
            HW_AVX512VL = (info[1] & ((int)1 << 31)) != 0;
            HW_AVX512BW = (info[1] & ((int)1 << 30)) != 0;
            HW_AVX512DQ = (info[1] & ((int)1 << 17)) != 0;
            HW_AVX512IFMA = (info[1] & ((int)1 << 21)) != 0;
            HW_AVX512VBMI = (info[2] & ((int)1 << 1)) != 0;*/
        }

        if (nExIds >= 0x80000001)
        {
            cpuid(info, 0x80000001);
            // HW_x64 = (info[3] & ((int)1 << 29)) != 0;
            // HW_ABM = (info[2] & ((int)1 << 5)) != 0;
            HW_SSE4a = (info[2] & ((int)1 << 6)) != 0;
            HW_FMA4 = (info[2] & ((int)1 << 16)) != 0;
            // HW_XOP = (info[2] & ((int)1 << 11)) != 0;
        }

        // End of code from answer

        sse41Supported = HW_SSE41;
        sse42Supported = HW_SSE42;
        sse4aSupported = HW_SSE4a;
        avx2Supported = HW_AVX2;
        avx512Supported = HW_AVX512F;
        bmi1Supported = HW_BMI1;
        fma3Supported = HW_FMA3;
        fma4Supported = HW_FMA4;

        // TODO: check / fix this
        // f16CSupported =

        // Following is code based on a different answer

        cpuid(info, 1);

        bool osUsesXSAVE_XRSTORE = (info[2] & (1 << 27)) != 0;
        if (osUsesXSAVE_XRSTORE && HW_AVX)
        {
#ifdef _MSC_VER
            unsigned long long xcrFeatureMask = _xgetbv(_XCR_XFEATURE_ENABLED_MASK);
#else
            unsigned long long xcrFeatureMask = _xgetbv(0);
#endif

            avxSupported = (xcrFeatureMask & 0x6) == 0x6;
        }
        else
        {
            avxSupported = false;
        }

        // Set sse off if any of the older SSE things are unset
        if (!HW_SSE || !HW_SSE2 || !HW_SSE3)
        {
            sse41Supported = false;
        }

        // If older AVX is not supported, turn off the newer as well
        if (!avxSupported)
            avx2Supported = false;
    }

private:
    static bool featuresRead;

    // Stored CPU flags
    static bool avxSupported;
    static bool sse41Supported;
    static bool sse42Supported;
    static bool sse4aSupported;

    static bool avx2Supported;
    static bool avx512Supported;

    static bool bmi1Supported;
    static bool fma3Supported;
    static bool fma4Supported;
};

inline bool CPUCheck::featuresRead = false;
inline bool CPUCheck::avxSupported = false;
inline bool CPUCheck::sse41Supported = false;
inline bool CPUCheck::sse42Supported = false;
inline bool CPUCheck::sse4aSupported = false;
inline bool CPUCheck::avx2Supported = false;
inline bool CPUCheck::avx512Supported = false;
inline bool CPUCheck::bmi1Supported = false;
inline bool CPUCheck::fma3Supported = false;
inline bool CPUCheck::fma4Supported = false;

} // namespace Thrive
