using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MelonLoader.Bootstrap.Utils;

public static partial class Dobby
{
    [LibraryImport("*", EntryPoint = "DobbyHook")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Hook(nint target, nint detour, ref nint original);

    [LibraryImport("*", EntryPoint = "DobbyDestroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Destroy(nint target);

    public static nint HookAttach(nint target, nint detour)
    {
        nint original = 0;
        if (Hook(target, detour, ref original) != 0)
        {
            throw new AccessViolationException($"Could not prepare patch to target {target:X}");
        }
        return original;
    }

    public static void HookDetach(nint target)
    {
        var result = Destroy(target);
        if (result is not 0 and not -1)
        {
            throw new AccessViolationException($"Could not destroy patch for target {target:X}");
        }
    }
}