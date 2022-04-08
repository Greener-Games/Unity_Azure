using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using UnityEngine;

namespace AzureHelpers
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
        
        public static async Task<BlobProperties> GetFileProperties(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await GetFileProperties(container, fileRoute);
        }

        /// <summary>
        ///    Get the property data for the Blob file.
        /// </summary>
        public static async Task<BlobProperties> GetFileProperties(BlobContainerClient container, string fileName)
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
        
        public static async Task<DateTime> LastModifiedAsync(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await LastModifiedAsync(container, fileRoute);
        }

        /// <summary>
        ///     Get the last modified time of a file in Azure async
        /// </summary>
        public static async Task<DateTime> LastModifiedAsync(BlobContainerClient container, string fileRoute)
        {
            try
            {
                BlobProperties properties = await GetFileProperties(container, fileRoute);

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
            BlobContainerClient container = await GetContainer(containerName, client);
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
                BlobProperties properties = await GetFileProperties(container, fileRoute);
                return properties.ContentLength;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static async Task<bool> FileExists(string containerName, string fileRoute, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await FileExists(container, fileRoute);
        }

        public static async Task<bool> FileExists(BlobContainerClient container, string fileName)
        {
            BlobClient blobClient = container.GetBlobClient(fileName);
            return await blobClient.ExistsAsync();
        }

        public static async Task<List<BlobItem>> ListBlobs(string containerName, string prefix = null, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await ListBlobs(container, prefix);
        }

        public static async Task<List<BlobItem>> ListBlobs(BlobContainerClient container, string prefix = null)
        {
            List<BlobItem> files = new List<BlobItem>();
            //Gets List of Blobs
            AsyncPageable<BlobItem> list = container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix);
            
            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobItem> blobPage in list.AsPages())
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    files.Add(blobItem);
                }
            }

            return files;
        }

        public static async Task<List<string>> GetTopLevelFolderStructure(BlobContainerClient blobContainerClient, string prefix = null)
        {
            List<string> returnValues = new List<string>();
            // Call the listing operation and return pages of the specified size.
            AsyncPageable<BlobHierarchyItem> resultSegment = blobContainerClient.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None,"/", prefix);
            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobHierarchyItem> blobPage in resultSegment.AsPages())
            {
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