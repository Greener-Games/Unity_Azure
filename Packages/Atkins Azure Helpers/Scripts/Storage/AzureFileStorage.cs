using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO.FileSystem;
using Azure;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using UnityEngine;

namespace AzureHelpers
{
    /// <summary>
    /// Class acting like an interface between ICON and Azure systems for uploading files.
    /// </summary>
    public partial class AzureFileStorage : MonoBehaviour
    {
        public enum BlobType
        {
            PAGE,
            APPEND,
            BLOCK,
        }
        
        static BlobServiceClient masterStorageAccount;
        public static BlobServiceClient MasterStorageAccount => masterStorageAccount ??= new BlobServiceClient(AzureSettings.StorageConnectionString, new BlobClientOptions(){Retry = {Delay = TimeSpan.FromSeconds(2), MaxRetries = 5, Mode = RetryMode.Fixed}});

        public static Dictionary<string, Dictionary<string, BlobContainerClient>> loadedContainers =
            new Dictionary<string, Dictionary<string, BlobContainerClient>>();

        public static BlobServiceClient GetServiceClient(string uri, AzureSasCredential credential)
        {
            return new BlobServiceClient(new Uri(uri), credential);
        }
        
        public static BlobServiceClient GetServiceClient(string connectionString)
        {
            return new BlobServiceClient(connectionString);
        }
        
        /// <summary>
        /// Static function returning the Container Task for a given name and connection string
        /// </summary>
        /// <param name="containerName">The name of the container we want to find</param>
        /// <returns></returns>
        public static async Task<BlobContainerClient> GetContainer(string containerName,string connectionString)
        {
            return await GetContainer(containerName, GetServiceClient(connectionString));
        }
        
        /// <summary>
        /// Static function returning the Container Task for a given name
        /// </summary>
        /// <param name="containerName">The name of the container we want to find</param>
        /// <returns></returns>
        public static async Task<BlobContainerClient> GetContainer(string containerName, BlobServiceClient client = null)
        {
            client ??= MasterStorageAccount;
            
            if (loadedContainers.ContainsKey(client.AccountName) && loadedContainers[client.AccountName].ContainsKey(containerName))
            {
                return loadedContainers[client.AccountName][containerName];
            }
            else
            {
                UnityEngine.Debug.Log($"loading container : {containerName} from {client.AccountName}");
                BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                if (!loadedContainers.ContainsKey(client.AccountName))
                {
                    loadedContainers.Add(client.AccountName, new Dictionary<string, BlobContainerClient>());
                }
                
                loadedContainers[client.AccountName].Add(containerName, containerClient);
            
                return containerClient;
            }
        }
        
        /// <summary>
        /// Function returning the Cloud Blob for a given container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        static async Task<BlobBaseClient> GetCloudBlob(BlobContainerClient container, string blobName)
        {
            Azure.Storage.Blobs.Models.BlobType blobType = (await GetFileProperties(container,blobName)).BlobType;
            BlobBaseClient cloudBlob;
            switch (blobType)
            {
                case Azure.Storage.Blobs.Models.BlobType.Append:
                    cloudBlob = container.GetAppendBlobClient(blobName);
                    break;
                case Azure.Storage.Blobs.Models.BlobType.Block:
                    cloudBlob = container.GetBlockBlobClient(blobName);
                    break;
                case Azure.Storage.Blobs.Models.BlobType.Page:
                    cloudBlob = container.GetPageBlobClient(blobName);
                    break;
                default:
                    throw new ArgumentException($"Invalid blob type {blobType.ToString()}", nameof(blobName));
            }

            return cloudBlob;
        }
        
        /// <summary>
        /// Delete a file from Azure async
        /// </summary>
        public static async Task<bool> DeleteFile(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await DeleteFile(container, fileRoute);
        }

        /// <summary>
        /// Delete a file from Azure async
        /// </summary>
        public static async Task<bool> DeleteFile(BlobContainerClient container, string fileRoute)
        {
            try
            {
                BlobBaseClient cloudBlob = await GetCloudBlob(container, fileRoute);
                
                bool exists = await cloudBlob.ExistsAsync();
                if (exists)
                {
                    await cloudBlob.DeleteAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return false;
            }
        }
    }
}