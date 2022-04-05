using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AsyncIO.FileSystem;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace AzureHelpers
{
    public partial class AzureFileStorage
    {
        public class DownloadOptions
        {
            public bool appendFilePath = true;
            public bool deleteFileIfExisting = true;

            public Action<ProgressRecorder> processCallback;
            
            public bool debug = true;

            public int bufferSize = 50 * 1024 * 1024; //50mb;
        }

        public class BulkDownloadOptions : DownloadOptions
        {
            public IProgress<string> bulkDownloadProgress;
            public int degreeOfParallelism = 8;
        }

        static DownloadOptions DefaultDownloadOptions => new DownloadOptions();

        /// <summary>
        /// Download a file from Azure server
        /// </summary>
        /// <param name="containerName">The name of the CloudBlobContainer</param>
        /// <param name="sourceFile">The blob name</param>
        /// <param name="saveFile">The path to the file we want to delete</param>
        /// <param name="client">Override to use a different client to the default one</param>
        /// <param name="downloadOptions"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadFile(string containerName, string sourceFile, string saveFile, DownloadOptions downloadOptions = null, BlobServiceClient client = null)
        {
            downloadOptions ??= DefaultDownloadOptions;
            
            BlobContainerClient container = await GetContainer(containerName, client);
            return await DownloadFile(container, sourceFile, saveFile, downloadOptions);
        }

        /// <summary>
        /// Download a file from Azure server
        /// </summary>
        /// <param name="container">The name of the CloudBlobContainer</param>
        /// <param name="sourceFile">The blob name</param>
        /// <param name="saveFile">The path to the file we want to delete</param>
        /// <param name="downloadOptions"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadFile(BlobContainerClient container, string sourceFile, string saveFile, DownloadOptions downloadOptions = null)
        {
            downloadOptions ??= DefaultDownloadOptions;
            
            try
            {
                // Download a blob to your file system
                string localPath = await ValidateSaveLocation(downloadOptions, sourceFile, saveFile);

                await Download(container, localPath, sourceFile, await CheckFileSize(container, sourceFile), downloadOptions);
                
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="sourceFiles"></param>
        /// <param name="saveLocation"></param>
        /// <param name="downloadOptions"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task<bool> BulkDownloadFiles(string containerName, IEnumerable<string> sourceFiles, string saveLocation, BulkDownloadOptions downloadOptions = null, BlobServiceClient client = null)
        {
            downloadOptions ??= DefaultDownloadOptions as BulkDownloadOptions;

            BlobContainerClient container = await GetContainer(containerName, client);
            return await BulkDownloadFiles(container, sourceFiles, saveLocation, downloadOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="sourceFiles"></param>
        /// <param name="saveLocation"></param>
        /// <param name="downloadOptions"></param>
        /// <returns></returns>
        public static async Task<bool> BulkDownloadFiles(BlobContainerClient container, IEnumerable<string> sourceFiles, string saveLocation, BulkDownloadOptions downloadOptions = null)
        {
            downloadOptions ??= DefaultDownloadOptions as BulkDownloadOptions;

            try
            {
                await GG.Extensions.AwaitExtensions.ForEachAsync(sourceFiles, downloadOptions.degreeOfParallelism, async (source, i) =>
                {
                    string localPath = await ValidateSaveLocation(downloadOptions, source, saveLocation);

                    if (!downloadOptions.deleteFileIfExisting && File.Exists(localPath))
                    {
                        return;
                    }
                    
                    await Download(container, localPath, source, 1, downloadOptions);
                }, downloadOptions.bulkDownloadProgress);
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            return false;
        }
        
        /// <summary>
        /// checks the file save location is valid and appends the total path if required,
        /// if the file already exists and is set to download will delete the current file
        /// </summary>
        /// <param name="options"></param>
        /// <param name="sourceLocation"></param>
        /// <param name="saveLocation"></param>
        /// <returns></returns>
        static async Task<string> ValidateSaveLocation(DownloadOptions options, string sourceLocation, string saveLocation)
        {
            // Download a blob to your file system
            string localPath = saveLocation;
            if (options.appendFilePath)
            {
                localPath = Path.Combine(saveLocation,sourceLocation);
            }

            if (options.deleteFileIfExisting && File.Exists(localPath))
            {
                await AsyncFile.DeleteAsync(localPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? string.Empty); //ensure the directory exists
            
            return localPath;
        }
        
        /// <summary>
        /// Runs the actual download for a file
        /// </summary>
        /// <param name="container"></param>
        /// <param name="localPath"></param>
        /// <param name="sourceFile"></param>
        /// <param name="totalSize"></param>
        /// <param name="downloadOptions"></param>
        static async Task Download(BlobContainerClient container, string localPath, string sourceFile, long totalSize, DownloadOptions downloadOptions)
        {
            if (!downloadOptions.deleteFileIfExisting && File.Exists(localPath))
            {
                return;
            }
            
            BlobBaseClient cloudBlob = await GetCloudBlob(container, sourceFile);
            Debug($"Downloading file {sourceFile} to {localPath}", downloadOptions.debug);
            using (FileStream outputFile = new FileStream(localPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                ProgressRecorder progressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceFile), totalSize)
                {
                    updateCallback = downloadOptions.processCallback
                };

                //Choose an appropriate buffer size
                byte[] downloadBuffer = new byte[downloadOptions.bufferSize];
                int bytesRead;
                int totalBytesDownloaded = 0;

                BlobDownloadInfo blobToDownload = (await cloudBlob.DownloadAsync()).Value;

                while ((bytesRead = await blobToDownload.Content.ReadAsync(downloadBuffer, 0, downloadBuffer.Length)) != 0)
                {
                    outputFile.Write(downloadBuffer, 0, bytesRead); // Write the download bytes from source stream to destination stream.
                    totalBytesDownloaded += bytesRead; //Increment the total downloaded counter. This is used for percentage calculation
                    progressHandler.Report(totalBytesDownloaded);
                }
            }
            Debug($"Download finished {sourceFile} to {localPath}", downloadOptions.debug);
        }
    }
}