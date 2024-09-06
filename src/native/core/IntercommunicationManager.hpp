#pragma once

#include "NativeLibIntercommunication.hpp"

namespace Thrive
{

/// \brief Manages handling NativeLibIntercommunication
class IntercommunicationManager
{
private:
    IntercommunicationManager() = default;

public:
    static IntercommunicationManager& Get() noexcept;

    [[nodiscard]] const NativeLibIntercommunication& GetIntercommunicationObject() const noexcept
    {
        return intercommunication;
    }

private:
    NativeLibIntercommunication intercommunication;
};

} // namespace Thrive
