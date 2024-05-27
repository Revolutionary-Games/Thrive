using System;

/// <summary>
///   CPU check result from the native library side. Must match CPUCheckResult.h (except the naming convention)
/// </summary>
[Flags]
public enum CPUCheckResult
{
    // ReSharper disable IdentifierTypo
    CPUCheckSuccess = 0,
    CPUCheckMissingAvx = 1,
    CPUCheckMissingSse41 = 2,
    CPUCheckMissingSse42 = 4,
    CPUCheckMissingAvx2 = 8,
    CPUCheckMissingLzcnt = 16,
    CPUCheckMissingBmi1 = 32,
    CPUCheckMissingFma = 64,
    CPUCheckMissingF16C = 128,

    // ReSharper restore IdentifierTypo
}
