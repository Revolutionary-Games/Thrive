// ------------------------------------ //
#include "core/IntercommunicationManager.hpp"

// ------------------------------------ //
namespace Thrive
{

IntercommunicationManager& IntercommunicationManager::Get() noexcept
{
    static IntercommunicationManager instance;
    return instance;
}

} // namespace Thrive
