using GG.ScriptableDataAsset;
using UnityEngine;

namespace Atkins.Azure
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
        
        /// <summary>
        /// The request done to Azure
        /// </summary>
        [SerializeField] public string connectionString;

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
        public static string ConnectionString => Loaded.connectionString;

        /// <summary>
        /// Function setting up the Asset
        /// </summary>
        /// <param name="asset"></param>
        protected override void NewAsset(AzureSettings asset)
        {
            base.NewAsset(asset);
            asset.connectionString =
                "DefaultEndpointsProtocol=http;AccountName=iconlivestorage;AccountKey=p2THVaXkAr0PGN9qnZhv2oSznC2AzjFcSwnfAm/R9tePdbp+uOYOjG2Bqx1B94yIXIMU6TUPdIbg65N8dUB/Iw==;EndpointSuffix=core.windows.net";
            ;
        }
    }
}