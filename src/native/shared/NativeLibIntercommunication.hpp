#pragma once

namespace Thrive
{

constexpr uint64_t INTEROP_MAGIC_VALUE = 42 * 42;

/// Contains pointers and other info passed through from ThriveNative to ThriveExtension during runtime setup phase
class NativeLibIntercommunication
{
public:
    NativeLibIntercommunication()
    {
        SanityCheckValue = INTEROP_MAGIC_VALUE;
    }

    uint64_t SanityCheckValue;
};

} // namespace Thrive
