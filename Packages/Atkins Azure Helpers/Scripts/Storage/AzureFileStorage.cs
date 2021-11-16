using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO.FileSystem;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using UnityEngine;

namespace Atkins.AzureHelpers
{
    /// <summary>
    /// Class acting like an interface between ICON and Azure systems for uploading files.
    /// </summary>
    public class AzureFileStorage : MonoBehaviour
    {
        public enum BlobType
        {
            PAGE,
            APPEND,
            BLOCK,
        }
        
        static BlobServiceClient _storageAccount;

        static BlobServiceClient StorageAccount => _storageAccount ?? (_storageAccount = new BlobServiceClient(AzureSettings.StorageConnectionString));
        
        /// <summary>
        /// Static function returning the Container Task for a given name
        /// </summary>
        /// <param name="containerName">The name of the container we want to find</param>
        /// <returns></returns>
        public static async Task<BlobContainerClient> GetContainer(string containerName)
        {
            BlobContainerClient containerClient = StorageAccount.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        /// <summary>
        /// Function returning the Cloud Blob for a given container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        static async Task<BlobBaseClient> GetCloudBlobAsync(BlobContainerClient container, string blobName)
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
        /// Upload a file to Azure server async
        /// </summary>
        /// <param name="containerName">The name of the CloudBlobContainer</param>
        /// <param name="sourceFile">The path of the file to upload</param>
        /// <param name="blobSaveLocation">The blob name</param>
        /// <param name="token">The cancellation token. Used only in TransferManager.UploadAsync method.</param>
        /// <param name="blobType">The type of the blob. By default: PAGE</param>
        /// <param name="processCallback">The progress callback element. Used for creating the progress recorder</param>
        /// <param name="tries"></param>
        /// <returns></returns>
        public static async Task<bool> UploadFile(string containerName, string sourceFile, string blobSaveLocation, CancellationToken token, BlobType blobType = BlobType.PAGE, Action<ProgressRecorder> processCallback = null, int tries = 0)
        {
            try
            {
                BlobContainerClient container = await GetContainer(containerName);
                await new WaitForUpdate();

                BlobBaseClient cloudBlob = blobType switch
                {
                    BlobType.PAGE => container.GetPageBlobClient(blobSaveLocation),
                    BlobType.APPEND => container.GetAppendBlobClient(blobSaveLocation),
                    BlobType.BLOCK => container.GetBlockBlobClient(blobSaveLocation),
                    _ => throw new ArgumentOutOfRangeException(nameof(blobType), blobType, null)
                };

                Debug.Log("Container :" + cloudBlob);
                Debug.Log($"Uploading file {sourceFile} to {blobSaveLocation}");

                FileInfo fileInfo = new FileInfo(sourceFile);
                long uploadFileSize = fileInfo.Length;
                ProgressRecorder progressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceFile), uploadFileSize)
                {
                    updateCallback = processCallback
                };

                using (MemoryStream ms = new MemoryStream())
                {
                    using (FileStream file = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[file.Length];
                        await file.ReadAsync(bytes, 0, (int)file.Length, token);
                        ms.Write(bytes, 0, (int)file.Length);
                    }

                    if (cloudBlob is BlockBlobClient b)
                    {
                        await b.UploadAsync(ms, null, null, null, null, progressHandler, token);
                    }
                    else if (cloudBlob is PageBlobClient p)
                    {
                        await p.UploadPagesAsync(ms, 0, null, null, progressHandler, token);
                    }
                    else if (cloudBlob is AppendBlobClient a)
                    {
                        await a.AppendBlockAsync(ms, null, null, progressHandler, token);
                    }
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.Log(e);

                return false;
            }
        }

        /// <summary>
        /// Download a file from Azure server async
        /// </summary>
        /// <param name="containerName">The name of the CloudBlobContainer</param>
        /// <param name="sourceLocation">The blob name</param>
        /// <param name="saveLocation">The path to the file we want to delete</param>
        /// <param name="processCallback">The progress callback</param>
        /// <param name="maxTries">Maximum numbers of tried before failing</param>
        /// <param name="currentTries">current number of tried for the download</param>
        /// <returns></returns>
        public static async Task<bool> DownloadFile(string containerName, string sourceLocation, string saveLocation, Action<ProgressRecorder> processCallback = null, int maxTries = 3, int currentTries = 0)
        {
            if (currentTries > maxTries)
            {
                return false;
            }

            try
            {
                BlobContainerClient container = await GetContainer(containerName);
                BlobBaseClient cloudBlob = await GetCloudBlobAsync(container, sourceLocation);
                await new WaitForUpdate();
                Debug.Log("Container :" + cloudBlob);

                // Download a blob to your file system
                string localPath = Path.Combine(saveLocation, sourceLocation);

                if (File.Exists(localPath))
                {
                    await AsyncFile.DeleteAsync(localPath);
                }

                Debug.Log($"Downloading file {sourceLocation} to {localPath}");

                using (FileStream outputFile = File.OpenWrite(localPath))
                {
                    ProgressRecorder progressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceLocation), await CheckFileSize(container, sourceLocation))
                    {
                        updateCallback = processCallback
                    };
                    
                    //Choose an appropriate buffer size
                    byte[] downloadBuffer = new byte[81920];
                    int bytesRead;
                    int totalBytesDownloaded = 0;

                    Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? string.Empty); //ensure the directory exists
                    BlobDownloadInfo blobToDownload = (await cloudBlob.DownloadAsync()).Value;
                    
                    while((bytesRead = await blobToDownload.Content.ReadAsync(downloadBuffer,0,downloadBuffer.Length))!=0)
                    {
                        outputFile.Write(downloadBuffer, 0, bytesRead); // Write the download bytes from source stream to destination stream.
                        totalBytesDownloaded += bytesRead;//Increment the total downloaded counter. This is used for percentage calculation
                        progressHandler.Report(totalBytesDownloaded);
                    }
                }
                
                Debug.Log($"Download finished {sourceLocation} to {localPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                currentTries ++;
                return await DownloadFile(containerName, sourceLocation, saveLocation, processCallback,maxTries, currentTries);
            }
            return false;
        }
        
        /// <summary>
        /// Delete a file from Azure async
        /// </summary>
        public static async Task<bool> DeleteFile(string containerName, string fileName)
        {
            try
            {
                BlobContainerClient container = await GetContainer(containerName);
                BlobBaseClient cloudBlob = await GetCloudBlobAsync(container, fileName);
                
                bool exists = await cloudBlob.ExistsAsync();
                if (exists)
                {
                    await cloudBlob.DeleteAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
        
       /// <summary>
       ///    Get the property data for the Blob file.
       /// </summary>
        public static async Task<BlobProperties> GetFileProperties(BlobContainerClient container, string fileName)
        {
            try
            {
                BlobBaseClient cloudBlob = await GetCloudBlobAsync(container, fileName);
                return await cloudBlob.GetPropertiesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new BlobProperties();
        }

        /// <summary>
        ///     Get the last modified time of a file in Azure async
        /// </summary>
        public static async Task<DateTime> LastModifiedAsync(BlobContainerClient container, string fileName)
        {
            try
            {
                BlobProperties properties = await GetFileProperties(container, fileName);

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
        
        /// <summary>
        /// Unused function?
        /// Function giving the size of a file
        /// </summary>
        /// <param name="container">The cloud blob container</param>
        /// <param name="fileName">The name of the file we want the size of</param>
        /// <returns>Returns the lenght of the file</returns>
        public static async Task<long> CheckFileSize(BlobContainerClient container, string fileName)
        {
            try
            {
                BlobProperties properties = await GetFileProperties(container, fileName);
                return properties.ContentLength;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}