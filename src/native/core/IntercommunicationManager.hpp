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

    [[nodiscard]] NativeLibIntercommunication& GetIntercommunicationObjectModifiable() noexcept
    {
        return intercommunication;
    }

    void ReportDebugDrawWorks()
    {
        intercommunication.PhysicsDebugSupported = true;
    }

private:
    NativeLibIntercommunication intercommunication;
};

} // namespace Thrive
