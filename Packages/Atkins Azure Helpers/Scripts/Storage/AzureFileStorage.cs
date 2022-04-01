using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO.FileSystem;
using Azure;
using Azure.Core;
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
        
        static BlobServiceClient masterStorageAccount;
        static BlobServiceClient MasterStorageAccount => masterStorageAccount ??= new BlobServiceClient(AzureSettings.StorageConnectionString, new BlobClientOptions(){Retry = {Delay = TimeSpan.FromSeconds(2), MaxRetries = 5, Mode = RetryMode.Fixed}});

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
            
            Debug.Log($"loading container {containerName} from {client.AccountName}");
            BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);
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

        public static async Task<bool> UploadFile(string containerName, string sourceFile, string blobLocation, CancellationToken token,
                                                  BlobType blobType = BlobType.PAGE, Action<ProgressRecorder> processCallback = null, int tries = 0, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await UploadFile(container, sourceFile, blobLocation, token, blobType, processCallback, tries);
        }

        /// <summary>
        /// Upload a file to Azure server async
        /// </summary>
        /// <param name="container">The Blob Container</param>
        /// <param name="sourceFile">The path of the file to upload</param>
        /// <param name="blobLocation">The blob name</param>
        /// <param name="token">The cancellation token. Used only in TransferManager.UploadAsync method.</param>
        /// <param name="blobType">The type of the blob. By default: PAGE</param>
        /// <param name="processCallback">The progress callback element. Used for creating the progress recorder</param>
        /// <param name="tries"></param>
        /// <returns></returns>
        public static async Task<bool> UploadFile(BlobContainerClient container, string sourceFile, string blobLocation, CancellationToken token, BlobType blobType = BlobType.PAGE, Action<ProgressRecorder> processCallback = null, int tries = 0)
        {
            try
            {
                await new WaitForUpdate();

                BlobBaseClient cloudBlob = blobType switch
                {
                    BlobType.PAGE => container.GetPageBlobClient(blobLocation),
                    BlobType.APPEND => container.GetAppendBlobClient(blobLocation),
                    BlobType.BLOCK => container.GetBlockBlobClient(blobLocation),
                    _ => throw new ArgumentOutOfRangeException(nameof(blobType), blobType, null)
                };

                Debug.Log("Container :" + cloudBlob);
                Debug.Log($"Uploading file {sourceFile} to {blobLocation}");

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
        /// <param name="sourceFile">The blob name</param>
        /// <param name="saveFile">The path to the file we want to delete</param>
        /// <param name="processCallback">The progress callback</param>
        /// <param name="maxTries">Maximum numbers of tried before failing</param>
        /// <param name="currentTries">current number of tried for the download</param>
        /// <returns></returns>
        public static async Task<bool> DownloadFile(string containerName,string sourceFile, string saveFile, bool appendFilePath = true, 
                                                    Action<ProgressRecorder> processCallback = null, BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainer(containerName, client);
            return await DownloadFile(container, sourceFile, saveFile, appendFilePath, processCallback);
        }

        /// <summary>
        /// Download a file from Azure server async
        /// </summary>
        /// <param name="container">The name of the CloudBlobContainer</param>
        /// <param name="sourceFile">The blob name</param>
        /// <param name="saveFile">The path to the file we want to delete</param>
        /// <param name="appendFilePath">append the source path onto the save file path</param>
        /// <param name="processCallback">The progress callback</param>
        /// <param name="maxTries">Maximum numbers of tried before failing</param>
        /// <param name="currentTries">current number of tried for the download</param>
        /// <returns></returns>
        public static async Task<bool> DownloadFile(BlobContainerClient container, string sourceFile, string saveFile, bool appendFilePath = true, Action<ProgressRecorder> processCallback = null)
        {
            try
            {
                BlobBaseClient cloudBlob = await GetCloudBlobAsync(container, sourceFile);
                await new WaitForUpdate();
                Debug.Log("Container :" + cloudBlob);

                // Download a blob to your file system
                string localPath = "";
                if (appendFilePath)
                {
                    localPath = Path.Combine(saveFile, sourceFile);
                }
                else
                {
                    localPath = saveFile;
                }

                if (File.Exists(localPath))
                {
                    await AsyncFile.DeleteAsync(localPath);
                }

                Debug.Log($"Downloading file {sourceFile} to {localPath}");

                Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? string.Empty); //ensure the directory exists
                using (FileStream outputFile = new FileStream(localPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    ProgressRecorder progressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceFile), await CheckFileSize(container, sourceFile))
                    {
                        updateCallback = processCallback
                    };
                    
                    //Choose an appropriate buffer size
                    byte[] downloadBuffer = new byte[81920];
                    int bytesRead;
                    int totalBytesDownloaded = 0;

                    BlobDownloadInfo blobToDownload = (await cloudBlob.DownloadAsync()).Value;
                    
                    while((bytesRead = await blobToDownload.Content.ReadAsync(downloadBuffer,0,downloadBuffer.Length))!=0)
                    {
                        outputFile.Write(downloadBuffer, 0, bytesRead); // Write the download bytes from source stream to destination stream.
                        totalBytesDownloaded += bytesRead;//Increment the total downloaded counter. This is used for percentage calculation
                        progressHandler.Report(totalBytesDownloaded);
                    }
                }
                
                Debug.Log($"Download finished {sourceFile} to {localPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            
            return false;
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
                BlobBaseClient cloudBlob = await GetCloudBlobAsync(container, fileRoute);
                
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
    }
}