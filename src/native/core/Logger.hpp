#pragma once

#include "Include.h"

namespace Thrive
{

enum class LogLevel : int8_t
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
};

/// \brief Provides native side logging support. Forwards logging to the usual Godot log
/// \todo For Visual Studio debugging implement the Windows debugger output writing when outputting to std::cout
class Logger
{
public:
    THRIVE_NATIVE_API static Logger& Get()
    {
        static Logger instance;
        return instance;
    }

    THRIVE_NATIVE_API inline void LogDebug(std::string_view message)
    {
        Log(message, LogLevel::Debug);
    }

    THRIVE_NATIVE_API inline void LogInfo(std::string_view message)
    {
        Log(message, LogLevel::Info);
    }

    THRIVE_NATIVE_API inline void LogWarning(std::string_view message)
    {
        Log(message, LogLevel::Warning);
    }

    THRIVE_NATIVE_API inline void LogError(std::string_view message)
    {
        Log(message, LogLevel::Error);
    }

    THRIVE_NATIVE_API void Log(std::string_view message, LogLevel level);

    /// \brief Sets the log level which controls what log messages actually get passed
    THRIVE_NATIVE_API void SetLogLevel(LogLevel level){
        currentLoggingLevel = level;
    }

    /// \brief Overrides the log output from standard to the given methods
    ///
    /// This is used by the managed side of things to setup logging
    /// \see CInterop.h
    THRIVE_NATIVE_API void SetLogTargetOverride(std::function<void(std::string_view, LogLevel)>&& logReceiver);

    /// \brief When flush on error is on, the output is flushed on each error message
    THRIVE_NATIVE_API void SetFlushOnError(bool flush){
        flushOnError = flush;
    }

private:
    bool flushOnError = true;

    bool isRedirected = false;
    std::function<void(std::string_view, LogLevel)> redirectedLogReceiver;

    LogLevel currentLoggingLevel = LogLevel::Info;
};

}; // namespace Thrive

#define LOG_DEBUG(x) Thrive::Logger::Get().LogDebug(x)
#define LOG_INFO(x) Thrive::Logger::Get().LogInfo(x)
#define LOG_WARNING(x) Thrive::Logger::Get().LogWarning(x)
#define LOG_ERROR(x) Thrive::Logger::Get().LogError(x)
