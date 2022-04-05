using GG.UnityEnsure;
using UnityEditor;

namespace AzureHelpers.Editor
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