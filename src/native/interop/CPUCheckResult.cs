using System;

/// <summary>
///   CPU check result from the native library side. Must match CPUCheckResult.h (except the naming convention)
/// </summary>
[Flags]
public enum CPUCheckResult
{
    CPUCheckSuccess = 0,
    CPUCheckMissingAvx = 1,
    CPUCheckMissingSse41 = 2,
    CPUCheckMissingSse42 = 4,
    CPUCheckMissingAvx2 = 8,
}
