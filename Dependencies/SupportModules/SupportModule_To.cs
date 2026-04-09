using System.Collections;
using UnityEngine;

namespace MelonLoader.Support
{
    internal class SupportModule_To : ISupportModule_To
    {
        public object StartCoroutine(IEnumerator coroutine)
        {
            if (!MelonCoroutines._hasProcessed
                || (Main.component == null))
            {
                MelonCoroutines._queue.Add(coroutine);
                return coroutine;
            }

#if SM_Il2Cpp
            return Main.component.StartCoroutine(new Il2CppSystem.Collections.IEnumerator(new MonoEnumeratorWrapper(coroutine).Pointer));
#else
            return Main.component.StartCoroutine(coroutine);
#endif
        }

        public void StopCoroutine(object coroutineToken)
        {
            if (!MelonCoroutines._hasProcessed
                || (Main.component == null))
            {
                MelonCoroutines._queue.Remove(coroutineToken as IEnumerator);
                return;
            }

            Main.component.StopCoroutine(coroutineToken as Coroutine);
        }

        public void UnityDebugLog(string msg) => Debug.Log(msg);
    }
}