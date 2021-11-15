using GG.ScriptableDataAsset;
using UnityEngine;
using UnityEngine.Serialization;

namespace Atkins.AzureHelpers
{
    /// <summary>
    /// A class creating the Azure settings 
    /// </summary>
    public class AzureSettings : ScriptableDataAsset<AzureSettings>
    {
        /// <summary>
        /// The name of the asset 
        /// </summary>
        protected override string AssetName { get; } = "Azure Settings";
        
        [SerializeField]public string dataSource;
        [SerializeField]public string databaseUserId;
        [SerializeField]public string databasePassword;
        [SerializeField]public string databaseName;
        
        
        /// <summary>
        /// The request done to Azure
        /// </summary>
        [SerializeField] public string storageConnectionString;

        /// <summary>
        /// A static reference to this DataAsset
        /// </summary>
        static AzureSettings _loaded;
        static AzureSettings Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    _loaded = LoadAsset();
                }

                return _loaded;
            }
        }

        /// <summary>
        /// A static reference to the connection string (The request we do to Azure)
        /// </summary>
        public static string StorageConnectionString => Loaded.storageConnectionString;

        /// <summary>
        /// Function setting up the Asset
        /// </summary>
        /// <param name="asset"></param>
        protected override void NewAsset(AzureSettings asset)
        {
            base.NewAsset(asset);
        }
    }
}