using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atkins.Azure.Editor
{
    /// <summary>
    /// Editor script used to enter settings for Azure
    /// </summary>
    class AzureSettingsProvider : SettingsProvider
    {
        /// <summary>
        /// The settings we are completing
        /// </summary>
        AzureSettings azureSettings;

        /// <summary>
        /// A sub-class organising styles
        /// </summary>
        class Styles
        {
            public static GUIContent stringStyle = new GUIContent("string");
        }

        /// <summary>
        /// Editor Constructor
        /// </summary>
        /// <param name="path"></param>
        /// <param name="scope"></param>
        public AzureSettingsProvider(string path, SettingsScope scope = SettingsScope.User): base(path, scope) {}
        
        /// <summary>
        /// Load assets on Activate
        /// </summary>
        /// <param name="searchContext">Not used</param>
        /// <param name="rootElement">Not used</param>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            azureSettings = AzureSettings.LoadAsset();
        }

        /// <summary>
        /// Function displaying a text field to enter the connection string
        /// </summary>
        /// <param name="searchContext">Not used</param>
        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            azureSettings.connectionString = EditorGUILayout.TextField("Storage Address", azureSettings.connectionString);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(azureSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Register the SettingsProvider
        /// </summary>
        /// <returns></returns>

        [SettingsProvider]
        public static SettingsProvider CreateAzureSettingsProvider()
        {
            var provider = new AzureSettingsProvider("Project/Atkins/Azure Settings", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };

            // Automatically extract all keywords from the Styles.
            return provider;
        }
    }
}