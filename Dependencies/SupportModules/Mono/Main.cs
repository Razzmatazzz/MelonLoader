using System;
using System.Reflection;
using MelonLoader.Support.Preferences;
using UnityEngine;

[assembly: MelonLoader.PatchShield]

namespace MelonLoader.Support
{
    internal static class Main
    {
        internal static ISupportModule_From Interface = null;
        internal static GameObject obj = null;
        internal static SM_Component component = null;

        private static ISupportModule_To Initialize(ISupportModule_From interface_from)
        {
            Interface = interface_from;
            UnityMappers.RegisterMappers();

            GetSceneManagerMethods(out MethodInfo sceneLoaded, 
                out MethodInfo sceneUnloaded);
            if (sceneLoaded == null)
            {
                MelonLogger.Warning("Failed to find Internal_SceneLoaded method");
                MelonLogger.Warning("Falling back to SupportModule Component Creation");
                SM_Component.Create();
            }
            else
                SceneHandler.Init(sceneLoaded, sceneUnloaded);

            return new SupportModule_To();
        }

        private static void GetSceneManagerMethods(out MethodInfo sceneLoaded,
            out MethodInfo sceneUnloaded)
        {
            sceneLoaded = null;
            sceneUnloaded = null;
            Type scenemanager = null;
            try
            {
                Assembly unityengine = Assembly.Load("UnityEngine.CoreModule");
                if (unityengine != null)
                    scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");

                if (scenemanager == null)
                {
                    unityengine = Assembly.Load("UnityEngine");
                    if (unityengine != null)
                        scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");
                }
            }
            catch { scenemanager = null; }
            if (scenemanager == null)
                return;

            sceneLoaded = scenemanager.GetMethod("Internal_SceneLoaded", BindingFlags.NonPublic | BindingFlags.Static);
            sceneUnloaded = scenemanager.GetMethod("Internal_SceneUnloaded", BindingFlags.NonPublic | BindingFlags.Static);
        }
    }
}