#pragma once

namespace Thrive
{

/// \brief Disables copying for all derived classes
class NonCopyable
{
protected:
    NonCopyable() = default;

public:
    NonCopyable(const NonCopyable& other) = delete;
    NonCopyable(NonCopyable&& other) = delete;

    NonCopyable operator=(const NonCopyable& other) = delete;
    NonCopyable operator=(NonCopyable&& other) = delete;
};

} // namespace Thrive
