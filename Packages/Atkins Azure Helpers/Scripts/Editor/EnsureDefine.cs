using GG.UnityEnsure;
using UnityEditor;

namespace Atkins.AzureHelpers.Editor
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