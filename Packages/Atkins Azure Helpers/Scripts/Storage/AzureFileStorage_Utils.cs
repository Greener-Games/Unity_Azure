using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using UnityEngine;

namespace Atkins.AzureHelpers
{
    public partial class AzureFileStorage
    {
        static void Debug(string message, bool debug)
        {
            if (debug)
            {
                UnityEngine.Debug.Log(message);
            }
        }
        
        public static async Task<BlobProperties> GetFilePropertiesAsync(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await GetFilePropertiesAsync(container, fileRoute);
        }

        /// <summary>
        ///    Get the property data for the Blob file.
        /// </summary>
        public static async Task<BlobProperties> GetFilePropertiesAsync(BlobContainerClient container, string fileName)
        {
            try
            {
                BlobBaseClient cloudBlob = container.GetBlobClient(fileName);
                return await cloudBlob.GetPropertiesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new BlobProperties();
        }

        public static BlobProperties GetFileProperties(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = GetContainer(containerName, client);
            return GetFileProperties(container, fileRoute);
        }

        /// <summary>
        ///    Get the property data for the Blob file.
        /// </summary>
        public static BlobProperties GetFileProperties(BlobContainerClient container, string fileName)
        {
            try
            {
                BlobBaseClient cloudBlob = container.GetBlobClient(fileName);
                return cloudBlob.GetProperties();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new BlobProperties();
        }

        public static async Task<DateTime> LastModifiedAsync(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await LastModifiedAsync(container, fileRoute);
        }

        /// <summary>
        ///     Get the last modified time of a file in Azure async
        /// </summary>
        public static async Task<DateTime> LastModifiedAsync(BlobContainerClient container, string fileRoute)
        {
            try
            {
                BlobProperties properties = await GetFilePropertiesAsync(container, fileRoute);

                if (properties.LastModified != null)
                {
                    return properties.LastModified.UtcDateTime;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return DateTime.MinValue;
        }

        public static async Task<long> CheckFileSize(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await CheckFileSize(container, fileRoute);
        }

        /// <summary>
        /// Unused function?
        /// Function giving the size of a file
        /// </summary>
        /// <param name="container">The cloud blob container</param>
        /// <param name="fileRoute">The name of the file we want the size of</param>
        /// <returns>Returns the lenght of the file</returns>
        public static async Task<long> CheckFileSize(BlobContainerClient container, string fileRoute)
        {
            try
            {
                BlobProperties properties = await GetFilePropertiesAsync(container, fileRoute);
                return properties.ContentLength;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static async Task<bool> FileExists(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await FileExists(container, fileRoute);
        }

        public static async Task<bool> FileExists(BlobContainerClient container, string fileName)
        {
            BlobClient blobClient = container.GetBlobClient(fileName);
            return await blobClient.ExistsAsync();
        }

        public static async Task<List<BlobItem>> ListBlobs(string containerName, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await ListBlobs(container);
        }

        public static async Task<List<BlobItem>> ListBlobs(BlobContainerClient container)
        {
            List<BlobItem> files = new List<BlobItem>();
            //Gets List of Blobs
            IAsyncEnumerable<Page<BlobItem>> list = container.GetBlobsAsync().AsPages(default, null);
            ;

            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobItem> blobPage in list)
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    files.Add(blobItem);
                }
            }

            return files;
        }

        public static async Task<List<string>> GetTopLevelFolderStructure(BlobContainerClient blobContainerClient)
        {
            List<string> returnValues = new List<string>();
            // Call the listing operation and return pages of the specified size.
            IAsyncEnumerable<Page<BlobHierarchyItem>> resultSegment = blobContainerClient.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/").AsPages(default);

            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobHierarchyItem> blobPage in resultSegment)
            {
                // A hierarchical listing may return both virtual directories and blobs.
                foreach (BlobHierarchyItem blobItem in blobPage.Values)
                {
                    if (blobItem.IsPrefix)
                    {
                        returnValues.Add(blobItem.Prefix);
                    }
                }
            }

            return returnValues;
        }
    }
}