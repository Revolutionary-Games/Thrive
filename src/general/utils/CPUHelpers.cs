using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Threading;

/// <summary>
///   Helpers for CPU operations across x86 and ARM
/// </summary>
public class CPUHelpers
{
    private static readonly bool IsX86 = X86Base.IsSupported;
    private static readonly bool IsARM = ArmBase.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HyperThreadPause()
    {
        if (IsX86)
        {
            X86Base.Pause();
        }
        else if (IsARM)
        {
            // This is almost equivalent to X86's Pause.
            // Warning: this is implemented as NOP on some CPUs, so it might not be the best option here.
            // This should be implemented as WFE and woke up by SEV, which would require custom bindings.
            ArmBase.Yield();
        }
        else
        {
            // Just to be sure.
            // This actually implements Pause on x86 and yield on ARM.
            Thread.SpinWait(1);
        }
    }
}
