#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AssetRipper.Primitives;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MelonLoader.InternalUtils;

namespace MelonLoader.Fixes.Il2CppInterop
{
    // Herp: This fixes an XRef issue with Il2CppInterop's GenericMethod::GetMethod Hook on some Unity Builds
    // Unity 2020.3.x
    // Unity 6000.x.x+
    internal static class Il2CppInteropGenericMethodGetMethodFix
    {
        private static MethodInfo _findTargetMethod;
        private static MethodInfo _findTargetMethod_Transpiler;

        private static UnityVersion _unity_6000 = new(6000);
        private static UnityVersion _unity_2020_3_48 = new(2020, 3, 48);
        private static UnityVersion _unity_2020_4 = new(2020, 4);

        internal static void Install()
        {
            if (UnityInformationHandler.EngineVersion < _unity_2020_3_48)
                return;

            if ((UnityInformationHandler.EngineVersion >= _unity_2020_4)
                && (UnityInformationHandler.EngineVersion < _unity_6000))
                return;

            try
            {
                Type thisType = typeof(Il2CppInteropGenericMethodGetMethodFix);
                Type classInjectorType = typeof(ClassInjector);
                Type enumerableType = typeof(Enumerable);

                Type hookType = classInjectorType.Assembly.GetType($"Il2CppInterop.Runtime.Injection.Hooks.GenericMethod_GetMethod_Hook");
                if (hookType == null)
                    throw new Exception($"Failed to get GenericMethod_GetMethod_Hook");

                _findTargetMethod = hookType.GetMethod("FindTargetMethod", BindingFlags.Public | BindingFlags.Instance);
                if (_findTargetMethod == null)
                    throw new Exception("Failed to get GenericMethod_GetMethod_Hook.FindTargetMethod");

                _findTargetMethod_Transpiler = thisType.GetMethod(nameof(FindTargetMethod_Transpiler), BindingFlags.NonPublic | BindingFlags.Static);

                MelonDebug.Msg($"Patching Il2CppInterop GenericMethod_GetMethod_Hook.FindTargetMethod...");
                Core.HarmonyInstance.Patch(_findTargetMethod,
                    null,
                    null,
                    new HarmonyMethod(_findTargetMethod_Transpiler));
            }
            catch (Exception e)
            {
                MelonLogger.Error(e);
            }
        }

        private static IEnumerable<CodeInstruction> FindTargetMethod_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var methods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public);
            var firstMethods = methods.Where(m => m.Name == "First" && m.GetParameters().Length == 1).ToList();
            int lastCallCount = 0;

            foreach (var inst in instructions)
            {
                if ((inst.opcode == OpCodes.Call) 
                    && (inst.operand is MethodInfo method)
                    && (method.Name == "Last"))
                {
                    lastCallCount++;

                    if (method.IsGenericMethod
                        && (lastCallCount == 2))
                    {
                        var genericType = method.GetGenericArguments()[0];
                        var newMethod = firstMethods.First().MakeGenericMethod(genericType);
                        inst.operand = newMethod;

                        MelonDebug.Msg("Patched Il2CppInterop GenericMethod_GetMethod_Hook getVirtualMethodXrefs.Last() -> getVirtualMethodXrefs.First()");
                    }
                }
            }

            return instructions;
        }
    }
}

#endif