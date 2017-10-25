#pragma once

// Common defines needed for thrive
#ifndef THRIVE_EXPORT
#ifdef _WIN32

#if THRIVELIB_BUILD == 1
#define THRIVE_EXPORT __declspec( dllexport )
#else
#define THRIVE_EXPORT __declspec( dllimport )
#endif

#else //_WIN32

#define THRIVELIB_BUILD
#endif  // THRIVELIB_BUILD == 1
#endif // THRIVE_EXPORT


