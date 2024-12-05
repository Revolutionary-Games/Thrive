using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

/// <summary>
///   Helpers for CPU operations across x86 and ARM
/// </summary>
public class CPUHelpers
{
    private static readonly bool Is86 = X86Base.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HyperThreadPause()
    {
        if (Is86)
        {
            X86Base.Pause();
        }
        else
        {
            // TODO: find a proper equivalent: https://github.com/Revolutionary-Games/Thrive/issues/5728
            // Thread.Yield();

            // Loop a tiny bit to waste a bit of time
            for (int i = 0; i < 100; ++i)
            {
            }
        }
    }
}
