// ------------------------------------ //
#include "Logger.hpp"

#include <iostream>

// ------------------------------------ //
namespace Thrive
{

void Logger::Log(std::string_view message, LogLevel level)
{
    // Drop logs that are not important enough with current level
    if (level < currentLoggingLevel)
        return;

    // TODO: should we have a mutex to lock before outputting?

    if (isRedirected)
    {
        redirectedLogReceiver(message, level);
        return;
    }

    if (level != LogLevel::Write)
    {
        std::cout << message << "\n";
    }
    else
    {
        std::cout << message;
    }

    if (level >= LogLevel::Error && flushOnError)
    {
        std::cout.flush();
    }
}

// ------------------------------------ //
void Logger::SetLogTargetOverride(std::function<void(std::string_view, LogLevel)>&& logReceiver)
{
    if (!logReceiver)
    {
        isRedirected = false;
        redirectedLogReceiver = nullptr;
    }
    else
    {
        redirectedLogReceiver = std::move(logReceiver);
        isRedirected = true;
    }
}

} // namespace Thrive
