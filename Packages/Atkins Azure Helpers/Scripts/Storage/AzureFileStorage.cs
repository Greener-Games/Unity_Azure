using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO.FileSystem;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
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

        static CloudStorageAccount _storageAccount;

        static CloudStorageAccount StorageAccount => _storageAccount ?? (_storageAccount = CloudStorageAccount.Parse(AzureSettings.StorageConnectionString));

        /// <summary>
        /// Static function returning the Container Task for a given name
        /// </summary>
        /// <param name="containerName">The name of the container we want to find</param>
        /// <returns></returns>
        public static async Task<CloudBlobContainer> GetContainer(string containerName)
        {
            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync(CancellationToken.None);

            return container;
        }

        /// <summary>
        /// Function returning the Cloud Blob for a given container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        static async Task<CloudBlob> GetCloudBlobAsync(CloudBlobContainer container, string blobName)
        {
            Microsoft.Azure.Storage.Blob.BlobType blobType = (await container.GetBlobReferenceFromServerAsync(blobName)).BlobType;
            CloudBlob cloudBlob;
            switch (blobType)
            {
                case Microsoft.Azure.Storage.Blob.BlobType.AppendBlob:
                    cloudBlob = container.GetAppendBlobReference(blobName);
                    break;
                case Microsoft.Azure.Storage.Blob.BlobType.BlockBlob:
                    cloudBlob = container.GetBlockBlobReference(blobName);
                    break;
                case Microsoft.Azure.Storage.Blob.BlobType.PageBlob:
                    cloudBlob = container.GetPageBlobReference(blobName);
                    break;
                case Microsoft.Azure.Storage.Blob.BlobType.Unspecified:
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
                CloudBlobContainer container = await GetContainer(containerName);
                await new WaitForUpdate();

                CloudBlob cloudBlob = null;
                switch (blobType)
                {
                    case BlobType.PAGE:
                        {
                            cloudBlob = container.GetPageBlobReference(blobSaveLocation);
                            break;
                        }

                    case BlobType.APPEND:
                        {
                            cloudBlob = container.GetAppendBlobReference(blobSaveLocation);
                            break;
                        }

                    case BlobType.BLOCK:
                        {
                            cloudBlob = container.GetBlockBlobReference(blobSaveLocation);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(blobType), blobType, null);
                }

                Debug.Log("Container :" + cloudBlob);
                Debug.Log($"Uploading file {sourceFile} to {blobSaveLocation}");

                TransferManager.Configurations.ParallelOperations = 64;

                SingleTransferContext context = new SingleTransferContext()
                {
                    ProgressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(containerName), new FileInfo(sourceFile).Length)
                    {
                        updateCallback = processCallback
                    },
                    ShouldOverwriteCallbackAsync = TransferContext.ForceOverwrite
                };

                UploadOptions options = new UploadOptions();


                using (MemoryStream ms = new MemoryStream())
                {
                    using (FileStream file = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[file.Length];
                        file.Read(bytes, 0, (int)file.Length);
                        ms.Write(bytes, 0, (int)file.Length);
                    }

                    await TransferManager.UploadAsync(ms, cloudBlob, options, context, token);
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
                CloudBlobContainer container = await GetContainer(containerName);
                CloudBlob cloudBlob = await GetCloudBlobAsync(container, sourceLocation);
                await new WaitForUpdate();
                Debug.Log("Container :" + cloudBlob);

                // Download a blob to your file system
                string path = Path.Combine(saveLocation, sourceLocation);

                if (File.Exists(path))
                {
                    await AsyncFile.DeleteAsync(path);
                }

                Debug.Log($"Downloading file {sourceLocation} to {path}");

                Directory.CreateDirectory(Path.GetDirectoryName(path)); //ensure the directory exists

                await cloudBlob.FetchAttributesAsync();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    TransferManager.Configurations.ParallelOperations = 64;

                    SingleTransferContext context = new SingleTransferContext()
                    {
                        ProgressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceLocation), cloudBlob)
                        {
                            updateCallback = processCallback
                        }
                    };
                    DownloadOptions options = new DownloadOptions
                    {
                        DisableContentMD5Validation = true
                    };

                    await TransferManager.DownloadAsync(cloudBlob, memoryStream, options, context);

                    using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                    {
                        memoryStream.WriteTo(file);
                    }

                    Debug.Log($"Download finished {sourceLocation} to {path}");
                }

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
       ///    Get the property data for the Blob file.
       /// </summary>
        public static async Task<BlobProperties> GetFileProperties(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer container = await GetContainer(containerName);
                CloudBlob cloudBlob = await GetCloudBlobAsync(container, fileName);
                
                await cloudBlob.FetchAttributesAsync();

                if (cloudBlob.Properties != null)
                {
                    return cloudBlob.Properties;
                }
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
        public static async Task<DateTime> LastModifiedAsync(string containerName, string fileName)
        {
            try
            {
                BlobProperties properties = await GetFileProperties(containerName, fileName);

                if (properties.LastModified != null)
                {
                    return properties.LastModified.Value.UtcDateTime;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Delete a file from Azure async
        /// </summary>
        public static async Task<bool> DeleteFile(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer container = await GetContainer(containerName);
                CloudBlob cloudBlob = await GetCloudBlobAsync(container, fileName);

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
        /// Unused function?
        /// Function giving the size of a file
        /// </summary>
        /// <param name="containerName">The cloud blob container</param>
        /// <param name="fileName">The name of the file we want the size of</param>
        /// <returns>Returns the lenght of the file</returns>
        public static async Task<long> CheckFileSize(string containerName, string fileName)
        {
            try
            {
                BlobProperties properties = await GetFileProperties(containerName, fileName);
                return properties.Length;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}