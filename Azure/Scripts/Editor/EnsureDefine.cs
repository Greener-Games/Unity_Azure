using GG.UnityEnsure;
using UnityEditor;

namespace Atkins.Azure.Editor
{
    internal class EnsureDefine
    {
        [InitializeOnLoadMethod]
        static void EnsureScriptingDefineSymbol()
        {
            EnsureUnityDefine.EnsureScriptingDefineSymbol("AZURE");
        }
    }
}